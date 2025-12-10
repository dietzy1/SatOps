package main

import (
	"bytes"
	"encoding/json"
	"flag"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"os/signal"
	"sync"
	"sync/atomic"
	"time"

	"github.com/gorilla/websocket"
)

const (
	defaultBaseURL = "http://localhost:5111"
	defaultWsURL   = "ws://localhost:5111"
)

// Configuration
var (
	baseURL           string
	wsURL             string
	numClients        int
	testDuration      time.Duration
	accessToken       string
	cleanupAfterTest  bool
	reportIntervalSec int
)

// Metrics
var (
	connectionsEstablished atomic.Int64
	connectionsFailed      atomic.Int64
	connectionsActive      atomic.Int64
	messagesReceived       atomic.Int64
	errors                 atomic.Int64
)

// API Types
type LocationDto struct {
	Latitude  float64 `json:"latitude"`
	Longitude float64 `json:"longitude"`
	Altitude  float64 `json:"altitude"`
}

type GroundStationCreateDto struct {
	Name     string      `json:"name"`
	Location LocationDto `json:"location"`
}

type GroundStationWithApiKeyDto struct {
	Id            int         `json:"id"`
	Name          string      `json:"name"`
	ApplicationId string      `json:"applicationId"`
	RawApiKey     string      `json:"rawApiKey"`
	Location      LocationDto `json:"location"`
	CreatedAt     time.Time   `json:"createdAt"`
}

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

type GroundStationCredentials struct {
	ID            int
	Name          string
	ApplicationID string
	ApiKey        string
}

func main() {
	// Parse command line arguments
	flag.StringVar(&baseURL, "base-url", defaultBaseURL, "Base URL of the SatOps API")
	flag.StringVar(&wsURL, "ws-url", defaultWsURL, "WebSocket URL of the SatOps API")
	flag.IntVar(&numClients, "clients", 10, "Number of concurrent WebSocket clients")
	flag.DurationVar(&testDuration, "duration", 5*time.Minute, "Duration of the test")
	flag.StringVar(&accessToken, "token", "", "Access token for creating ground stations (required)")
	flag.BoolVar(&cleanupAfterTest, "cleanup", true, "Delete created ground stations after test")
	flag.IntVar(&reportIntervalSec, "report-interval", 10, "Interval in seconds for status reports")
	flag.Parse()

	if accessToken == "" {
		log.Fatal("Error: --token is required. Please provide an access token for the SatOps platform.")
	}

	log.Println("╔══════════════════════════════════════════════════════════════╗")
	log.Println("║          SatOps WebSocket Load Test                          ║")
	log.Println("╚══════════════════════════════════════════════════════════════╝")
	log.Printf("Configuration:")
	log.Printf("  • Base URL: %s", baseURL)
	log.Printf("  • WebSocket URL: %s", wsURL)
	log.Printf("  • Number of clients: %d", numClients)
	log.Printf("  • Test duration: %s", testDuration)
	log.Printf("  • Cleanup after test: %v", cleanupAfterTest)
	log.Println()

	// Step 1: Create ground stations
	log.Println("Step 1: Creating ground station credentials...")
	credentials, err := createGroundStations(numClients)
	if err != nil {
		log.Fatalf("Failed to create ground stations: %v", err)
	}
	log.Printf("✓ Successfully created %d ground stations\n", len(credentials))

	// Step 2: Launch WebSocket connections
	log.Println("\nStep 2: Launching WebSocket connections...")

	var wg sync.WaitGroup
	stopChan := make(chan struct{})

	// Handle interrupt signal
	interrupt := make(chan os.Signal, 1)
	signal.Notify(interrupt, os.Interrupt)

	// Start metrics reporter
	go reportMetrics(stopChan, reportIntervalSec)

	// Launch all clients
	for i, cred := range credentials {
		wg.Add(1)
		go func(clientID int, cred GroundStationCredentials) {
			defer wg.Done()
			runClient(clientID, cred, stopChan)
		}(i+1, cred)
		// Small delay between client launches to avoid thundering herd
		time.Sleep(100 * time.Millisecond)
	}

	// Wait for test duration or interrupt
	select {
	case <-time.After(testDuration):
		log.Println("\n⏱️  Test duration completed")
	case <-interrupt:
		log.Println("\n⚠️  Received interrupt signal")
	}

	// Signal all clients to stop
	close(stopChan)

	// Wait for all clients to finish
	log.Println("Waiting for all clients to disconnect...")
	wg.Wait()

	// Print final results
	printFinalResults()

	// Cleanup
	if cleanupAfterTest {
		log.Println("\nStep 3: Cleaning up ground stations...")
		cleanupGroundStations(credentials)
	}

	log.Println("\n Load test completed!")
}

func createGroundStations(count int) ([]GroundStationCredentials, error) {
	credentials := make([]GroundStationCredentials, 0, count)

	for i := 0; i < count; i++ {
		name := fmt.Sprintf("LoadTest-GS-%d", i+1)

		// Create unique locations for each ground station
		createDto := GroundStationCreateDto{
			Name: name,
			Location: LocationDto{
				Latitude:  55.0 + float64(i)*0.1, // Spread across Denmark
				Longitude: 12.0 + float64(i)*0.1,
				Altitude:  100.0,
			},
		}

		jsonData, err := json.Marshal(createDto)
		if err != nil {
			return nil, fmt.Errorf("failed to marshal request: %w", err)
		}

		req, err := http.NewRequest("POST", baseURL+"/api/v1/ground-stations", bytes.NewBuffer(jsonData))
		if err != nil {
			return nil, fmt.Errorf("failed to create request: %w", err)
		}
		req.Header.Set("Content-Type", "application/json")
		req.Header.Set("Authorization", "Bearer "+accessToken)

		resp, err := http.DefaultClient.Do(req)
		if err != nil {
			return nil, fmt.Errorf("failed to create ground station %s: %w", name, err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusCreated {
			body, _ := io.ReadAll(resp.Body)
			return nil, fmt.Errorf("failed to create ground station %s: status %d, body: %s", name, resp.StatusCode, string(body))
		}

		var gsResponse GroundStationWithApiKeyDto
		if err := json.NewDecoder(resp.Body).Decode(&gsResponse); err != nil {
			return nil, fmt.Errorf("failed to decode response for %s: %w", name, err)
		}

		credentials = append(credentials, GroundStationCredentials{
			ID:            gsResponse.Id,
			Name:          gsResponse.Name,
			ApplicationID: gsResponse.ApplicationId,
			ApiKey:        gsResponse.RawApiKey,
		})

		log.Printf("  ✓ Created ground station: %s (ID: %d)", name, gsResponse.Id)
	}

	return credentials, nil
}

func runClient(clientID int, cred GroundStationCredentials, stopChan <-chan struct{}) {
	log.Printf("[Client %d] Starting for ground station: %s", clientID, cred.Name)

	// Get WebSocket token
	token, err := getWebSocketToken(cred.ApplicationID, cred.ApiKey)
	if err != nil {
		log.Printf("[Client %d] Failed to get token: %v", clientID, err)
		connectionsFailed.Add(1)
		errors.Add(1)
		return
	}

	// Connect to WebSocket
	conn, err := connectWebSocket(token)
	if err != nil {
		log.Printf("[Client %d] Failed to connect WebSocket: %v", clientID, err)
		connectionsFailed.Add(1)
		errors.Add(1)
		return
	}
	defer conn.Close()

	connectionsEstablished.Add(1)
	connectionsActive.Add(1)
	defer connectionsActive.Add(-1)

	log.Printf("[Client %d] ✓ WebSocket connected", clientID)

	// Read messages until stop signal
	messageChan := make(chan []byte)
	errorChan := make(chan error)

	go func() {
		for {
			_, message, err := conn.ReadMessage()
			if err != nil {
				errorChan <- err
				return
			}
			messageChan <- message
		}
	}()

	// Set up ping ticker to keep connection alive
	pingTicker := time.NewTicker(30 * time.Second)
	defer pingTicker.Stop()

	for {
		select {
		case <-stopChan:
			log.Printf("[Client %d] Received stop signal, closing connection", clientID)
			conn.WriteMessage(websocket.CloseMessage, websocket.FormatCloseMessage(websocket.CloseNormalClosure, ""))
			return
		case msg := <-messageChan:
			messagesReceived.Add(1)
			log.Printf("[Client %d] 📩 Received message: %s", clientID, truncateString(string(msg), 100))
		case err := <-errorChan:
			if websocket.IsCloseError(err, websocket.CloseNormalClosure, websocket.CloseGoingAway) {
				log.Printf("[Client %d] Connection closed normally", clientID)
			} else {
				log.Printf("[Client %d] ❌ WebSocket error: %v", clientID, err)
				errors.Add(1)
			}
			return
		case <-pingTicker.C:
			if err := conn.WriteMessage(websocket.PingMessage, nil); err != nil {
				log.Printf("[Client %d] ❌ Failed to send ping: %v", clientID, err)
				errors.Add(1)
				return
			}
		}
	}
}

func getWebSocketToken(applicationID, apiKey string) (string, error) {
	reqBody := TokenRequest{
		ApplicationID: applicationID,
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

	// Read confirmation
	_, confirmMsg, err := conn.ReadMessage()
	if err != nil {
		conn.Close()
		return nil, err
	}

	log.Printf("Server confirmation: %s", truncateString(string(confirmMsg), 100))

	return conn, nil
}

func cleanupGroundStations(credentials []GroundStationCredentials) {
	for _, cred := range credentials {
		req, err := http.NewRequest("DELETE", fmt.Sprintf("%s/api/v1/ground-stations/%d", baseURL, cred.ID), nil)
		if err != nil {
			log.Printf("Failed to create delete request for %s: %v", cred.Name, err)
			continue
		}
		req.Header.Set("Authorization", "Bearer "+accessToken)

		resp, err := http.DefaultClient.Do(req)
		if err != nil {
			log.Printf("Failed to delete ground station %s: %v", cred.Name, err)
			continue
		}
		resp.Body.Close()

		if resp.StatusCode == http.StatusNoContent || resp.StatusCode == http.StatusOK {
			log.Printf("  ✓ Deleted ground station: %s", cred.Name)
		} else {
			log.Printf("Failed to delete ground station %s: status %d", cred.Name, resp.StatusCode)
		}
	}
}

func reportMetrics(stopChan <-chan struct{}, intervalSec int) {
	ticker := time.NewTicker(time.Duration(intervalSec) * time.Second)
	defer ticker.Stop()

	startTime := time.Now()

	for {
		select {
		case <-stopChan:
			return
		case <-ticker.C:
			elapsed := time.Since(startTime).Round(time.Second)
			log.Printf("[%s] Active: %d | Established: %d | Failed: %d | Messages: %d | Errors: %d",
				elapsed,
				connectionsActive.Load(),
				connectionsEstablished.Load(),
				connectionsFailed.Load(),
				messagesReceived.Load(),
				errors.Load(),
			)
		}
	}
}

func printFinalResults() {
	log.Println("\nLoad Test Summary:")
	log.Printf("║  Connections Established: %-35d ║", connectionsEstablished.Load())
	log.Printf("║  Connections Failed:      %-35d ║", connectionsFailed.Load())
	log.Printf("║  Total Messages Received: %-35d ║", messagesReceived.Load())
	log.Printf("║  Total Errors:            %-35d ║", errors.Load())

	// Determine test pass/fail
	if connectionsFailed.Load() == 0 && errors.Load() == 0 && connectionsEstablished.Load() == int64(numClients) {
		log.Println("\nTEST PASSED: All clients connected successfully with no errors!")
	} else {
		log.Println("\nTEST FAILED: Some connections failed or errors occurred.")
	}
}

func truncateString(s string, maxLen int) string {
	if len(s) <= maxLen {
		return s
	}
	return s[:maxLen] + "..."
}
