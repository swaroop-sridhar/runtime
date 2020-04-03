// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <memory>
#include "extractor.h"
#include "runner.h"
#include "trace.h"
#include "header.h"
#include "manifest.h"
#include "utils.h"

using namespace bundle;

// This method processes the bundle manifest.
// It also implements the extraction of files that cannot be directly processed from the bundle.
StatusCode runner_t::extract()
{
    try
    {
        const int8_t* addr = map_bundle();

        // Set the Reader offset to read post the bundle header
        reader_t reader(addr, m_bundle_size, m_header_offset + m_header.size());

        // Read the bundle manifest
        m_manifest = manifest_t::read(reader, m_header.num_embedded_files());

        // Extract the files if necessary
        if (m_manifest.files_need_extraction())
        {
            extractor_t extractor(m_header.bundle_id(), m_bundle_path, m_manifest);
            m_extraction_path = extractor.extract(reader);
        }

        unmap_bundle(addr);

        return StatusCode::Success;
    }
    catch (StatusCode e)
    {
        return e;
    }
}

const file_entry_t*  runner_t::probe(const pal::string_t& path) const
{
    for (const file_entry_t& entry : m_manifest.files)
    {
        if (entry.relative_path() == path)
        {
            return &entry;
        }
    }

    return nullptr;
}

bool runner_t::locate(const pal::string_t& relative_path, pal::string_t& full_path) const
{
    const bundle::runner_t* app = bundle::runner_t::app();
    const bundle::file_entry_t* entry = app->probe(relative_path);

    if (entry == nullptr)
    {
        return false;
    }

    bool needs_extraction = entry->needs_extraction();
    pal::string_t file_base = entry->needs_extraction() ? app->extraction_path() : app->base_path();

    full_path.assign(file_base);
    append_path(&full_path, relative_path.c_str());

    return true;
}
