cmake_minimum_required(VERSION 3.20)
project(aubio_wrapper LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

# vcpkg toolchain
set(VCPKG_ROOT "$ENV{VCPKG_ROOT}")
if(NOT VCPKG_ROOT)
    set(VCPKG_ROOT "C:/Users/cato/scoop/apps/vcpkg/current")
endif()
set(VCPKG_INSTALLED "${VCPKG_ROOT}/installed/x64-windows")

# Create shared library
add_library(aubio_wrapper SHARED
    aubio_wrapper.c
)

# Include directories
target_include_directories(aubio_wrapper PRIVATE
    "${VCPKG_INSTALLED}/include"
)

# Link aubio static lib and its dependencies
target_link_directories(aubio_wrapper PRIVATE
    "${VCPKG_INSTALLED}/lib"
)

target_link_libraries(aubio_wrapper PRIVATE
    aubio
    sndfile
    FLAC
    FLAC++
    ogg
    vorbis
    vorbisenc
    opus
    mpg123
    libmp3lame
    avcodec
    avformat
    avutil
    swresample
    bz2
    lzma
    ws2_32
    secur32
    bcrypt
    mfuuid
    strmiids
)

# Export symbols
target_compile_definitions(aubio_wrapper PRIVATE
    AUBIO_WRAPPER_EXPORTS
)

# Output to runtimes directory for .NET native deployment
set_target_properties(aubio_wrapper PROPERTIES
    OUTPUT_NAME "aubio_wrapper"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../../runtimes/win-x64/native"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../../runtimes/win-x64/native"
)

# Install target
install(TARGETS aubio_wrapper
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
)
