#!/bin/bash

# Clean working directories

docker run --rm -v "$PWD"/../gpmf:/gpmf -w /gpmf/demo gcc:4.9 make clean

rm -rf build
rm -rf ./SkiConsole/build
echo "All clean"
