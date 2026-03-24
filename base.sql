-- Active: 1774366055843@@127.0.0.1@5432@gomoku_db
CREATE DATABASE gomoku_db;

USE gomoku_db;

CREATE TABLE partie (
    id SERIAL PRIMARY KEY,
    player1 VARCHAR(50) NOT NULL,
    player2 VARCHAR(50) NOT NULL,
    grid_size INT DEFAULT 15,
    date_creation TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE actions (
    id SERIAL PRIMARY KEY,
    partie_id INT REFERENCES partie(id),
    player_name VARCHAR(50), -- "J1" ou "J2" ou le nom
    x INT NOT NULL,
    y INT NOT NULL,
    tour_numero INT NOT NULL, -- 1, 2, 3...
    type_action VARCHAR(10) -- 'POINT' ou 'BOMBE'
);