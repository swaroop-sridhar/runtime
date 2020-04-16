# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

project(static_apphost)
set(DOTNET_PROJECT_NAME "static_apphost")

# Add RPATH to the apphost binary that allows using local copies of shared libraries
# dotnet core depends on for special scenarios when system wide installation of such
# dependencies is not possible for some reason.
# This cannot be enabled for MacOS (Darwin) since its RPATH works in a different way,
# doesn't apply to libraries loaded via dlopen and most importantly, it is not transitive.
if (NOT CLR_CMAKE_TARGET_OSX)
    set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
    set(CMAKE_INSTALL_RPATH "\$ORIGIN/netcoredeps")
endif()

set(SKIP_VERSIONING 1)

include_directories(..)
include_directories(../../json)

set(SOURCES
    ../bundle_marker.cpp
    ./hostfxr_iface.cpp
    ./hostpolicy_resolver.cpp
)

set(HEADERS
    ../bundle_marker.h
    ../../../hostfxr_iface.h
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        static_apphost.def)
else(CLR_CMAKE_TARGET_WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/static_apphost_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/static_apphost.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    if(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD)
        # Add linker exports file option
        set(EXPORTS_LINKER_OPTION -Wl,--version-script=${EXPORTS_FILE})
    endif(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD)

    if(CLR_CMAKE_HOST_OSX)
        # Add linker exports file option
        set(EXPORTS_LINKER_OPTION -Wl,-exported_symbols_list,${EXPORTS_FILE})
    endif(CLR_CMAKE_HOST_OSX)

    if(CLR_CMAKE_HOST_SUNOS)
        # Add linker exports file option
        set(EXPORTS_LINKER_OPTION -Wl,-M,${EXPORTS_FILE})
    endif(CLR_CMAKE_HOST_SUNOS)
endif(CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        ../apphost.windows.cpp)

    list(APPEND HEADERS
        ../apphost.windows.h)
endif()

include(../../exe.cmake)

add_definitions(-DFEATURE_APPHOST=1)
add_definitions(-DFEATURE_STATIC_HOST=1)

# Disable manifest generation into the file .exe on Windows
if(CLR_CMAKE_TARGET_WIN32)
    set_property(TARGET ${PROJECT_NAME} PROPERTY
            LINK_FLAGS "/MANIFEST:NO"
        )
endif()

set_property(TARGET static_apphost PROPERTY ENABLE_EXPORTS 1)

if(CLR_CMAKE_HOST_UNIX)
    add_custom_target(static_apphost_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(static_apphost static_apphost_exports)

    set_property(TARGET static_apphost APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET static_apphost APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_UNIX)

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (CLR_CMAKE_TARGET_WIN32 AND (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64))
    target_link_libraries(static_apphost Advapi32.lib shell32.lib)
endif()

target_link_libraries(static_apphost
    libhostfxr_static
    libhostpolicy_static
    libhostcommon
)
