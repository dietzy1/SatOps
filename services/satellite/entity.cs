/* CREATE TABLE IF NOT EXISTS
    Satellites (
        id SERIAL PRIMARY KEY,
        name VARCHAR(255) NOT NULL,
        norad_id INT UNIQUE, -- Standard satellite identifier
        tle_line1 VARCHAR(69), -- Two-Line Element set
        tle_line2 VARCHAR(69),
        tle_updated_at TIMESTAMP,
        is_active BOOLEAN DEFAULT TRUE,
        created_at TIMESTAMP DEFAULT NOW()
    ); */

