# ProjFutur

This project is a minimal RESTful Web API built using .NET 8.0. It was designed for managing user profiles, handling authentication, and managing a player's (user's) inventory.

## Specification
Use cases covered:
- Adding a single item to a user's inventory
- Adding multiple items to a user's inventory
- Fetching a user's inventory
- Subtracting from a user's inventory
  
Where the following is also true:
- A user can have 0 of an item
- A user must only be able to edit their own inventory
- A user's inventory must recognize that new item types could be added later on
- A user's inventory must survive if the service goes down

## Overview
- **JWT Authentication**: Secure access to API endpoints using JSON Web Tokens
- **User Management**: Endpoints for creating, retrieving, and managing user profiles
- **Inventory Management**: Endpoints for adding, updating, and managing items in a user's inventory
- **Interactive API Documentation**: Documentation is provided via [ReDocly](https://redocly.com), allowing you to explore and test the API endpoints

## Key Features

- **Secure Authentication**: Utilises JWT tokens for secure and stateless authentication
- **User Profile Management**: Create, retrieve, and manage user profiles
- **Inventory Operations**: Add, update, and manage inventory items provided an authenticated user
- **ReDocly Documentation**: Interactive API documentation for easy exploration of endpoints
- **Docker Compose Setup**: Easily set up the application and its dependencies

## Getting Started

### Prerequisites

Before you start, please make sure you have the following installed:

- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) - For building and running the application
- [Docker](https://www.docker.com/get-started) - (Optional) For containerising the application and PostgreSQL database
- [PostgreSQL](https://www.postgresql.org/download/) - (if not using Docker Compose) for managing the database
- [Postman](https://www.postman.com/downloads/) or [Insomnia](https://insomnia.rest/download) - (Optional) For API testing, where you can import the `proj-futur-api-endpoints-export-insomnia-or-postman.json` file for a pre-configured collection of API endpoints

## Running the Project with Docker

If you prefer to use Docker, you can start the application and its dependencies using Docker Compose. Follow these steps:

1. **Build and Start the Services**

   Navigate to the project directory and run the following command:

   ```bash
   docker-compose up --build
2. **Alternatively**

   If you'd like to get the project's dependencies (Postgres) only and run the project locally, you can use the following command:

   ```bash
   docker-compose pull
3. **pgAdmin**

    One image is pgAdmin which can be used to manage the database. The login credentials for this are defined in the <code>docker-compose.yml</code>file.

## Configuring PostgreSQL (if not using Docker Compose)
To setup this project without Docker, the only dependency for this project is [PostgreSQL](https://www.postgresql.org/download/), from there a localhost server can be setup, where the <code>appsettings.json</code> Postgres connection string can be updated to point to this.

# Next steps?
The focus of this project was creating a well-documented Inventory API that a game client might use. It also implemented JWT authentication, which turned out to be tightly coupled to the API itself, and 'bloated' the docs. This could be exposed as part of its own API and consumed by any other API, agnostic of language. It would also build on the microservice concepts illustrated here, in that changes made to the game client will not break the authentication API, thus making it easier to test, extend -- more loosely coupled, produce less bugs, overall allowing the focus to be on making a highly resilient inventory management system for a game client. 
