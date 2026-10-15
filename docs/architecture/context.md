# System Context (C4 Level 1)

> High-level view of the CryptoMarket platform and its interactions with human users and external systems.

---

## Context Diagram

The CryptoMarket platform provides cryptocurrency trading simulation, portfolio tracking, limit order automation, AI-driven market news generation, and real-time push alerts.

```mermaid
flowchart TB
    %% Last verified against commit: b5229aa

    subgraph Users ["Users"]
        Trader["Crypto Trader / User<br/>[Person]<br/>Trades crypto, manages wallets, sets limit orders and price alerts"]
    end

    subgraph System ["CryptoMarket Platform"]
        Platform["CryptoMarket System<br/>[.NET 9 Microservices]<br/>Handles authentication, market simulation, wallet management, trading, and alerts"]
    end

    subgraph External ["External Services"]
        Gemini["Google Gemini API<br/>[External Service]<br/>Generates synthetic crypto market news using AI"]
        SMTP["SMTP Server<br/>[External Service]<br/>Delivers transactional welcome and alert emails"]
    end

    Trader -->|"Browses market, executes trades, views portfolio, configures alerts (HTTPS / WSS)"| Platform
    Platform -->|"Generates market news via prompt requests (HTTPS REST)"| Gemini
    Platform -->|"Sends transactional user emails (SMTP)"| SMTP

    classDef person fill:#08427b,stroke:#052e56,color:#ffffff,rx:6
    classDef system fill:#1168bd,stroke:#0b4884,color:#ffffff,rx:6
    classDef external fill:#999999,stroke:#666666,color:#ffffff,rx:6

    class Trader person
    class Platform system
    class Gemini,SMTP external
```

*Last verified against commit: `b5229aa`*

---

## External Systems & Actors

| Actor / System | Type | Role & Interaction Description |
|---|---|---|
| **Crypto Trader / User** | Human Actor | Interacts with the platform via Web UI (Vue 3) or Mobile Client (Flutter) through the API Gateway. Authenticates via JWT, monitors real-time prices, manages asset holdings, executes trades, places limit orders, and receives live notifications. |
| **Google Gemini API** | External System | REST API invoked by `Market.API` (`GeminiService`) to dynamically generate crypto market news articles based on current market dynamics. |
| **SMTP Server** | External System | Mail server used by `Notifications.API` (`EmailService`) to send welcome emails upon account registration. |

---

## Next Steps

- For container-level decomposition, see [Containers (C4 Level 2)](containers.md).
- For local execution setup, see [Running Locally](../operations/running-locally.md).

---

## Sources

- `CRYPTOMARKET_DOCS_IMPLEMENTATION_PLAN.md` — Phase 2
- `docs/_inventory.md` — Section 1 (Services) & Section 6 (Service-to-service HTTP calls)
- `README.md` — Architecture Overview
