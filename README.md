# dpulogin
Automatic Login For DPU Network for server deployments on campus
## STILL IN DEVELOPMENT. DONT USE.


# Usage:

set environment variables for "DPU_USER" and "DPU_PASS" with:

`setx DPU_USER "user@dpu.edu.tr"`
and
`setx DPU_PASS "password"`

p.s.: to set the env variables at machine level, use the /M feature flag.

then to actually run this as a windows service in the background, run:

`sc create "DPULogin" binpath="path/to/dpulogin.exe" start=auto`

then start the service:
`sc start "DPULogin"`
