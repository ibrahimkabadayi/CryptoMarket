#  CryptoMarket - Microservices Trading Platform

[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Supported-blue.svg)](https://www.docker.com/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Event%20Driven-FF6600.svg)](https://www.rabbitmq.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

A modern, highly scalable, and event-driven microservices architecture simulating a cryptocurrency and asset trading platform. Built with **ASP.NET Core 9**, this project demonstrates enterprise-level backend engineering practices including Clean Architecture, asynchronous inter-service communication, and containerization.

##  Architecture & Tech Stack

This platform is composed of loosely coupled, independent microservices communicating via an API Gateway and an Event Bus.

* **API Gateway:** YARP (Yet Another Reverse Proxy)
* **Message Broker:** RabbitMQ (via MassTransit) for pub/sub event-driven communication.
* **Databases:** * **PostgreSQL:** Relational data management for Identity and Portfolio services (via Entity Framework Core).
    * **MongoDB:** NoSQL document storage for high-frequency Market data (Generic Repository pattern).
    * **Redis:** In-memory caching for real-time market prices *(In progress)*.
* **Containerization:** Docker & Docker Compose.
* **Security:** JWT (JSON Web Token) Authentication.

##  Microservices Overview

| Service | Port (Internal) | Database | Description |
| :--- | :--- | :--- | :--- |
| **API.Gateway** | `5000` | - | The single entry point for all client requests. Handles routing to internal microservices. |
| **Identity.API** | `8080` | PostgreSQL | Manages user authentication, registration, and JWT generation. Publishes `UserCreatedEvent`. |
| **Portfolio.API** | `8080` | PostgreSQL | Manages user wallets, fiat balances, and asset transactions. Consumes events to auto-generate Ethereum-style wallet addresses for new users. |
| **Market.API** | `8080` | MongoDB | Stores cryptocurrency/precious metal details, real-time prices, and historical data. |
| **Notifications.API**| `8080` | - | *(Upcoming)* Real-time user notifications using SignalR WebSockets. |

##  Event-Driven Flow Example

The system utilizes **MassTransit** to ensure eventual consistency without tight coupling. 
For example, the User Registration flow:
1. Client sends a `POST /api/Auth/register` request through the **API Gateway**.
2. **Identity.API** saves the user to PostgreSQL and publishes a `UserCreatedEvent` to RabbitMQ.
3. Both **Portfolio.API** and **Market.API** consume this event independently.
4. **Portfolio.API** automatically generates a unique `0x...` wallet address and creates a zero-balance database record for the new user.

##  Getting Started

### Prerequisites
* [Docker Desktop](https://www.docker.com/products/docker-desktop) installed and running.
* .NET 9 SDK (for local development/migrations).

### Installation & Running

1. **Clone the repository:**
   ```bash
   git clone [https://github.com/yourusername/CryptoMarket.git](https://github.com/yourusername/CryptoMarket.git)
   cd CryptoMarket
