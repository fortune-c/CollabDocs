# 30-Day Roadmap: Real-Time Collaborative Document Backend

This roadmap guides the development of a **Real-Time Collaborative
Document System (Mini Google Docs Backend)** using:

-   ASP.NET Core (.NET 8)
-   SignalR (WebSockets)
-   PostgreSQL
-   Redis
-   Docker

Goal: Build a **production-style backend** that demonstrates real-time
systems, concurrency handling, and distributed architecture.

------------------------------------------------------------------------

# Week 1 --- Backend Foundations

## Day 1 --- Project Setup

Tasks: - Install .NET 8 SDK - Install Docker - Install PostgreSQL -
Install Redis - Create ASP.NET Web API project

Command:

``` bash
dotnet new webapi -n CollabDocs
```

Learn: - ASP.NET Core project structure - Dependency Injection

------------------------------------------------------------------------

## Day 2 --- Clean Architecture Structure

Create project folders:

    CollabDocs
     ├── Api
     ├── Domain
     ├── Infrastructure
     ├── Services

Learn: - Layered architecture - Separation of concerns

------------------------------------------------------------------------

## Day 3 --- PostgreSQL Setup

Install EF Core provider:

``` bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

Create tables: - Users - Documents - DocumentPermissions

Learn: - ORM vs SQL - EF Core migrations

------------------------------------------------------------------------

## Day 4 --- User Registration

Endpoint:

    POST /auth/register

Add password hashing using:

-   BCrypt.Net

Learn: - Password hashing - Secure authentication design

------------------------------------------------------------------------

## Day 5 --- Login + JWT Authentication

Endpoint:

    POST /auth/login

Return JWT token.

Learn: - JWT tokens - Authentication middleware

------------------------------------------------------------------------

## Day 6 --- Authorization (RBAC)

Roles:

-   Owner
-   Editor
-   Viewer

Learn: - Role-Based Access Control

------------------------------------------------------------------------

## Day 7 --- Document CRUD API

Endpoints:

    POST /documents
    GET /documents
    GET /documents/{id}
    DELETE /documents/{id}

Learn: - REST API design - DTO patterns

------------------------------------------------------------------------

# Week 2 --- Real-Time Infrastructure

## Day 8 --- Install SignalR

Install:

``` bash
dotnet add package Microsoft.AspNetCore.SignalR
```

Learn: - WebSockets - Real-time communication

------------------------------------------------------------------------

## Day 9 --- Create Document Hub

Create:

    DocumentHub.cs

Example method:

    JoinDocument(documentId)

Learn: - SignalR hubs - Connection lifecycle

------------------------------------------------------------------------

## Day 10 --- Real-Time Messaging

Implement:

    SendEditOperation

Broadcast edits to connected users.

Learn: - Publish/subscribe messaging

------------------------------------------------------------------------

## Day 11 --- Document Rooms

Use SignalR groups so users editing the same document join the same
room.

Learn: - Room-based messaging

------------------------------------------------------------------------

## Day 12 --- Store Document Content

Create table:

-   DocumentVersions

Learn: - Version storage strategies

------------------------------------------------------------------------

## Day 13 --- Apply Edit Operations

Supported operations:

-   Insert
-   Delete
-   Replace

Example:

``` json
{
  "type": "insert",
  "position": 5,
  "text": "hello"
}
```

Learn: - Operation-based editing

------------------------------------------------------------------------

## Day 14 --- Save Edit Logs

Create table:

-   DocumentEdits

Learn: - Event sourcing basics

------------------------------------------------------------------------

# Week 3 --- Concurrency & Conflict Resolution

## Day 15 --- Study Concurrent Editing

Concepts:

-   Operational Transform (OT)
-   CRDT

------------------------------------------------------------------------

## Day 16 --- Implement Basic Operational Transform

Handle simultaneous edits like:

User A inserts at position 5\
User B inserts at position 5

Learn: - Transformation algorithms

------------------------------------------------------------------------

## Day 17 --- Server-Side Operation Queue

Create an edit queue so operations process sequentially.

Learn: - Concurrency control - Queues

------------------------------------------------------------------------

## Day 18 --- Document State Cache

Store active documents in memory.

Tool:

-   IMemoryCache

Learn: - Caching strategies

------------------------------------------------------------------------

## Day 19 --- Redis Setup

Run Redis with Docker:

``` bash
docker run redis
```

Learn: - Redis basics - Pub/Sub messaging

------------------------------------------------------------------------

## Day 20 --- Redis Pub/Sub

Publish document edits to Redis so other servers receive them.

Learn: - Distributed messaging

------------------------------------------------------------------------

## Day 21 --- Horizontal Scaling

Configure SignalR Redis backplane.

Learn: - Horizontal scaling - Multi-server architecture

------------------------------------------------------------------------

# Week 4 --- Production Features

## Day 22 --- Document Version History

Endpoint:

    GET /documents/{id}/history

Learn: - Snapshot strategy

------------------------------------------------------------------------

## Day 23 --- Restore Version

Endpoint:

    POST /documents/{id}/restore

Learn: - Rollback systems

------------------------------------------------------------------------

## Day 24 --- Rate Limiting

Tool:

-   AspNetCoreRateLimit

Learn: - API protection

------------------------------------------------------------------------

## Day 25 --- Logging

Tool:

-   Serilog

Learn: - Structured logging

------------------------------------------------------------------------

## Day 26 --- Monitoring

Tools:

-   Prometheus
-   Grafana

Learn: - Metrics and observability

------------------------------------------------------------------------

## Day 27 --- Load Testing

Tools:

-   k6
-   Locust

Learn: - Performance testing

------------------------------------------------------------------------

## Day 28 --- Dockerize System

Create:

    docker-compose.yml

Services:

-   API
-   PostgreSQL
-   Redis

Learn: - Container orchestration

------------------------------------------------------------------------

## Day 29 --- Architecture Documentation

Add to README:

-   Architecture diagram
-   API documentation
-   Scaling strategy

Tool:

-   draw.io

------------------------------------------------------------------------

## Day 30 --- Final Polish

Add:

-   Deployment guide
-   Demo video
-   Screenshots
-   Benchmark results

Optional deployment:

-   Azure
-   DigitalOcean
