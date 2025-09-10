/* CREATE TYPE user_role AS ENUM('admin', 'scientist', 'viewer');

CREATE TABLE IF NOT EXISTS
    Users (
        id SERIAL PRIMARY KEY,
        name VARCHAR(255) NOT NULL,
        email VARCHAR(255) NOT NULL UNIQUE,
        role user_role NOT NULL
    ); */

