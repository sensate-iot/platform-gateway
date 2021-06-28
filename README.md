# Sensate IoT - Gateway API

![header1] ![header2] ![header3]

This is the platform gateway solution for the Sensate IoT data platform. This
solution contains the platform/network gateway API:

- Gateway API
- Authorization service

## Gateway

The gateway is the entry point to the platform. All other ingress services forward
data to this gateway internally. The gateway performs message authentication. The
authorization of messages is done by the router.

[header1]: https://github.com/sensate-iot/platform-gateway/workflows/Docker/badge.svg "Docker Build"
[header2]: https://github.com/sensate-iot/platform-gateway/workflows/Format%20check/badge.svg ".NET format"
[header3]: https://img.shields.io/badge/version-v1.0.2-informational "Sensate IoT version"
