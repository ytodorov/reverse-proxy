# Load Balancer

This load balancer is a simple yet powerful implementation in C# using the .NET framework. It is designed to distribute HTTP traffic across multiple servers using a round-robin algorithm that evenly distributes traffic across all available servers. The load balancer is capable of handling high levels of traffic without becoming a bottleneck. In this documentation the terms Reverse Proxy and Load Balancer means pretty much the same thing.

## Features

- Evenly distribute traffic across multiple servers using a round-robin algorithm
- Handle high levels of HTTP traffic
- Health checks for monitoring server status and automatically removing failed servers from the pool
- Sticky sessions support to route requests from the same client to the same server

## Prerequisites

- .NET SDK (version 7.0 or higher)
- Visual Studio or any other .NET-compatible IDE

## Getting Started

1. Clone the repository:

\```
git clone https://github.com/ytodorov/reverse-proxy.git
\```

2. Change to the project directory:

\```
cd reverse-proxy
\```

3. Open the solution file in Visual Studio or your preferred .NET-compatible IDE.

4. Build the solution to ensure that all dependencies are properly installed.

## Configuration

1. Open the `appsettings.json` file located in the root of the project.

2. Update the `ServerUris` section with the list of servers you want the load balancer to manage:

\```json
"ServerUris": [
"http://testapi-yordan-1.azurewebsites.net/",
"http://testapi-yordan-2.azurewebsites.net/",
"http://testapi-yordan-3.azurewebsites.net/"
],
\```

3. Configure the `EnableStickySession` section if you want to enable sticky sessions:

\```json
"EnableStickySession" : false
\```

- Set to `true` to enable sticky sessions, `false` to disable them.

4. Save your changes to the configuration file.

## Running the Load Balancer

1. Set the ReverseProxy.MinimalApi project as the startup project in your IDE.

2. Run the project. The load balancer will start listening for incoming HTTP requests and forward them to the configured servers.

## Running Unit Tests

The solution includes a set of unit tests to demonstrate the functionality and reliability of the load balancer. To run the tests, follow these steps:

1. In your IDE, open the Test Explorer.

2. Run all tests. The Test Explorer will display the results of the tests.
