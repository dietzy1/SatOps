package main

import (
	"bytes"
	"encoding/binary"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"mime/multipart"
	"net/http"
	"os"
	"os/signal"
	"time"

	"github.com/gorilla/websocket"
	"google.golang.org/protobuf/proto"
)

const (
	baseURL = "http://localhost:5111"
	wsURL   = "ws://localhost:5111"
)

type TokenRequest struct {
	ApplicationID string `json:"applicationId"`
	ApiKey        string `json:"apiKey"`
}

type TokenResponse struct {
	AccessToken string `json:"accessToken"`
}

type WebSocketConnectMessage struct {
	Type  string `json:"type"`
	Token string `json:"token"`
}

type WebSocketScheduleTransmissionMessage struct {
	RequestID string                            `json:"requestId"`
	Type      string                            `json:"type"`
	Frames    int                               `json:"frames"`
	Data      WebSocketScheduleTransmissionData `json:"data"`
}

type WebSocketScheduleTransmissionData struct {
	Satellite       string `json:"satellite"`
	Time            string `json:"time"`
	FlightPlanID    int    `json:"flightPlanId"`
	SatelliteID     int    `json:"satelliteId"`
	GroundStationID int    `json:"groundStationId"`
}

func main() {
	log.Println("Step 1: Authenticating with ground station credentials...")
	token, err := getToken()
	if err != nil {
		log.Fatalf("Failed to get token: %v", err)
	}
	log.Printf("✓ Successfully obtained access token: %s...\n", token[:20])

	log.Println("\nStep 2: Connecting to WebSocket...")
	conn, err := connectWebSocket(token)
	if err != nil {
		log.Fatalf("Failed to connect to WebSocket: %v", err)
	}
	defer conn.Close()
	log.Println("✓ WebSocket connection established")

	interrupt := make(chan os.Signal, 1)
	signal.Notify(interrupt, os.Interrupt)

	done := make(chan struct{})

	go func() {
		defer close(done)
		for {
			_, message, err := conn.ReadMessage()
			if err != nil {
				log.Println("WebSocket read error:", err)
				return
			}
			log.Printf("📩 Received WebSocket message: %s\n", string(message))

			var scheduleMsg WebSocketScheduleTransmissionMessage
			if err := json.Unmarshal(message, &scheduleMsg); err == nil && scheduleMsg.Type == "schedule_transmission" {
				log.Printf("📋 Parsed schedule transmission for satellite '%s' (ID: %d), Flight Plan: %d, Ground Station: %d\n",
					scheduleMsg.Data.Satellite,
					scheduleMsg.Data.SatelliteID,
					scheduleMsg.Data.FlightPlanID,
					scheduleMsg.Data.GroundStationID)

				_, _, _ = conn.ReadMessage()

				log.Println("\n📦 Packing Binary Container (Meta + Image)...")
				if err := sendImage(token, scheduleMsg.Data.GroundStationID, scheduleMsg.Data.SatelliteID, scheduleMsg.Data.FlightPlanID); err != nil {
					log.Printf("⚠ Failed to send image: %v\n", err)
				} else {
					log.Println("✓ Binary container sent successfully")
				}
			}
		}
	}()

	log.Println("Press Ctrl+C to exit")

	select {
	case <-done:
		log.Println("WebSocket connection closed")
	case <-interrupt:
		log.Println("\nReceived interrupt signal, closing connection...")
		conn.WriteMessage(websocket.CloseMessage, websocket.FormatCloseMessage(websocket.CloseNormalClosure, ""))
		time.Sleep(time.Second)
	}
}

func getToken() (string, error) {
	appID := os.Getenv("GS_APP_ID")
	apiKey := os.Getenv("GS_API_KEY")

	if appID == "" || apiKey == "" {
		return "", fmt.Errorf("GS_APP_ID and GS_API_KEY environment variables must be set")
	}

	reqBody := TokenRequest{
		ApplicationID: appID,
		ApiKey:        apiKey,
	}

	jsonData, err := json.Marshal(reqBody)
	if err != nil {
		return "", err
	}

	resp, err := http.Post(
		baseURL+"/api/v1/ground-station-link/token",
		"application/json",
		bytes.NewBuffer(jsonData),
	)
	if err != nil {
		return "", err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return "", fmt.Errorf("token request failed with status %d: %s", resp.StatusCode, string(body))
	}

	var tokenResp TokenResponse
	if err := json.NewDecoder(resp.Body).Decode(&tokenResp); err != nil {
		return "", err
	}

	return tokenResp.AccessToken, nil
}

func connectWebSocket(token string) (*websocket.Conn, error) {
	conn, _, err := websocket.DefaultDialer.Dial(wsURL+"/api/v1/ground-station-link/connect", nil)
	if err != nil {
		return nil, err
	}

	connect := WebSocketConnectMessage{
		Type:  "connect",
		Token: token,
	}

	if err := conn.WriteJSON(connect); err != nil {
		conn.Close()
		return nil, err
	}

	_, confirmMsg, err := conn.ReadMessage()
	if err != nil {
		conn.Close()
		return nil, err
	}

	log.Printf("Server confirmation: %s\n", string(confirmMsg))

	return conn, nil
}

func sendImage(token string, groundStationID, satelliteID, flightPlanID int) error {
	imageData, err := os.ReadFile("memesat-1.png")
	if err != nil {
		return fmt.Errorf("failed to read memesat-1.png: %w", err)
	}

	// Create Protobuf Metadata
	metadata := &Metadata{
		Size:      int32(len(imageData)),
		Height:    1000,
		Width:     1000,
		Channels:  3,
		Timestamp: int32(time.Now().Unix()),
		BitsPixel: 8,
		Camera:    "Mock-Cam-Go",
		Obid:      int32(flightPlanID),
		Items: []*MetadataItem{
			{
				Key: "prediction",
				Value: &MetadataItem_IntValue{
					IntValue: 1, // Mock: AI detected a ship
				},
			},
			{
				Key: "confidence",
				Value: &MetadataItem_FloatValue{
					FloatValue: 0.99,
				},
			},
		},
	}

	// Serialize Metadata to bytes
	metaBytes, err := proto.Marshal(metadata)
	if err != nil {
		return fmt.Errorf("failed to marshal protobuf: %v", err)
	}

	// Build the Binary Container
	// Structure: [4 bytes Size (LE)] [Metadata Bytes] [Image Bytes]
	containerBuffer := new(bytes.Buffer)

	// Write Metadata Size (4 bytes, Little Endian)
	metaSize := int32(len(metaBytes))
	if err := binary.Write(containerBuffer, binary.LittleEndian, metaSize); err != nil {
		return fmt.Errorf("failed to write meta size: %v", err)
	}

	containerBuffer.Write(metaBytes)

	containerBuffer.Write(imageData)

	log.Printf("  -> Constructed container: Header(%d) + Meta(%d) + Image(%d) = Total(%d)",
		4, len(metaBytes), len(imageData), containerBuffer.Len())

	// Create Multipart Request
	var bodyBuf bytes.Buffer
	writer := multipart.NewWriter(&bodyBuf)

	writer.WriteField("SatelliteId", fmt.Sprintf("%d", satelliteID))
	writer.WriteField("GroundStationId", fmt.Sprintf("%d", groundStationID))
	writer.WriteField("FlightPlanId", fmt.Sprintf("%d", flightPlanID))
	writer.WriteField("CaptureTime", time.Now().UTC().Format(time.RFC3339))
	writer.WriteField("Latitude", "55.676098")
	writer.WriteField("Longitude", "12.568337")

	part, err := writer.CreateFormFile("ImageFile", "satellite_data.png")
	if err != nil {
		return err
	}

	// Write the binary container, not just the image
	part.Write(containerBuffer.Bytes())

	writer.Close()

	req, err := http.NewRequest("POST", baseURL+"/api/v1/ground-station-link/images", &bodyBuf)
	if err != nil {
		return err
	}

	req.Header.Set("Content-Type", writer.FormDataContentType())
	req.Header.Set("Authorization", "Bearer "+token)

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("image upload failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}
