// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __RUNNER_H__
#define __RUNNER_H__

#include "error_codes.h"
#include "header.h"
#include "manifest.h"
#include "info.h"

// bundle::runner extends bundle::info to supports:
// * Reading the bundle manifest and identifying file locations for the runtime
// * Extracting bundled files to disk when necessary
// bundle::runner is used by HostPolicy.

namespace bundle
{
    class runner_t : public info_t
    {
    public:
        runner_t(const pal::char_t* bundle_path,
                 const pal::char_t *app_path,
                 int64_t header_offset)
            : info_t(bundle_path, app_path, header_offset) {}

        const pal::string_t& extraction_path() const { return m_extraction_path; }

        const file_entry_t *probe(const pal::char_t* relative_path) const;
        bool locate(const pal::string_t& relative_path, pal::string_t& full_path) const;

        static StatusCode process_manifest_and_extract()
        {
            return ((runner_t*) the_app)->extract();
        }

        static const runner_t* app() { return (const runner_t*)the_app; }

        // Probe the app-bundle for the file 'path' and return its location ('offset', 'size') if found.
        // This method is intnded to be used by the runtime to probe for bundled assemblies
        // This method assumes that the currently executing app is a single-file bundle.
        static bool bundle_probe(const char* path, int64_t *offset, int64_t *size );

        // If this app is a single-file bundle, returns bundle_probe function pointer encoded as a string.
        // If not, returns nullptr.
        static const char* get_bundle_probe();

    private:

        StatusCode extract();

        manifest_t m_manifest;
        pal::string_t m_extraction_path;
    };
}

#endif // __RUNNER_H__
