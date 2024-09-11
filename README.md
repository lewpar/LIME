# LIME
**L**ightweight **I**nterface for **M**anaging **E**ndpoints (LIME) is endpoint management tool split up into 3 main components:
- Mediator - The mediator server the agents report to.
- Agent - The service/daemon that runs on the endpoints.
- CLI - Tool designed to create the certificates for communication.

> [!CAUTION]
> This tool is written to help manage my local network devices and improve on my ASP.NET and network programming knowledge and is not expected to be secure or complete.
> Use at your own risk.

## Installation
Like mentioned earlier LIME is made up of three main components, however the setup is split into two components:
- Server (Mediator)
- Client (Agent)

### Server Setup
The server uses a two-tier Public Key Infrastructure (PKI) setup for verifying identities. 
The LIME.CLI tool is a command line tool for helping setup this PKI, including creating the root certificate and 
intermediate certificate authorities. 

- Install the LIME.CLI tool on an air-gapped device (this is where you will issue your intermediate certificate)
- Create a root and intermediate certificate (run `./lime --help` on the CLI tool for guidance)
- Install the `intermediate.private.chain.p12` certificate on the server hosting the mediator.
- Install the LIME.Mediator server on the target server you installed the certificate on.
- Run the mediator and observe it asks for the thumbprint of the certificates you installed.

### Client Setup
- Launch the mediator server and visit the address `https://localhost:55124`.
- Select `Create Agent` on the dashboard to start creating a new agent.
- Fill in the details for the endpoint that will be hosting the agent.
- Copy the certificate to the LIME.Agent root directory and name it `agent.pfx` so it gets auto-imported by the agent.

## Dependencies
- EntityFrameworkCore
- Pomelo.EntityFrameworkCore.MySql
- xUnit
