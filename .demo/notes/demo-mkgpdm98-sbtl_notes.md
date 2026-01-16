# Notes for slides 1-3 specific issues

Bacs probably my worst ever dev ex

Bacs (60odd SF microservices) + 6 more SF deployments + 5 databases for integration tests to pass
54 service bus queues!
Took weeks, constantly finding more required config and more missing queues and topics

Managing git changes – don’t check in secrets!

Config options (launchsettings, appsettings.development.json, VS secrets, cmd line args, env variables) 
how to choose? 
Sometimes we have combinations of multiple!
Some options are more or less discoverable than others

Service bus
Constant put request failed etc
Requires automation!