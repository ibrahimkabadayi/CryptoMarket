# API Documentation & OpenAPI Specifications

> Index of OpenAPI specifications, endpoint catalogs, and communication contracts for the CryptoMarket platform.

---

## API References

| Document / Resource | Description | Format |
|---|---|---|
| [Endpoints Reference](endpoints.md) | Master endpoint tables, request/response models, and error handling contracts | Markdown |
| [Identity OpenAPI Spec](openapi-identity.json) | Authentication and user identity OpenAPI 3.0 specification | JSON |
| [Market OpenAPI Spec](openapi-market.json) | Market listings, candlestick history, and news OpenAPI 3.0 specification | JSON |
| [Notifications OpenAPI Spec](openapi-notifications.json) | Push notifications and price alerts OpenAPI 3.0 specification | JSON |
| [Portfolio OpenAPI Spec](openapi-portfolio.json) | Wallets, trading, and limit order engine OpenAPI 3.0 specification | JSON |

---

## Live OpenAPI Schemas (Development)

When running the platform locally, ASP.NET Core OpenApi automatically serves live schemas at:
- `Identity.API`: `http://localhost:8001/openapi/v1.json`
- `Market.API`: `http://localhost:8002/openapi/v1.json`
- `Notifications.API`: `http://localhost:6000/openapi/v1.json`
- `Portfolio.API`: `http://localhost:7000/openapi/v1.json`

---

## Sources

- [endpoints.md](endpoints.md)
- [openapi-identity.json](openapi-identity.json)
- [openapi-market.json](openapi-market.json)
- [openapi-notifications.json](openapi-notifications.json)
- [openapi-portfolio.json](openapi-portfolio.json)
