// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "dir_utils.h"
#include "error_codes.h"
#include "utils.h"

using namespace bundle;

bool dir_utils_t::has_dirs_in_path(const pal::string_t& path)
{
    return path.find_last_of(DIR_SEPARATOR) != pal::string_t::npos;
}

void dir_utils_t::create_directory_tree(const pal::string_t &path)
{
    if (path.empty())
    {
        return;
    }

    if (pal::directory_exists(path))
    {
        return;
    }

    if (has_dirs_in_path(path))
    {
        create_directory_tree(get_directory(path));
    }

    if (!pal::mkdir(path.c_str(), 0700)) // Owner - rwx
    {
        if (pal::directory_exists(path))
        {
            // The directory was created since we last checked.
            return;
        }

        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Failed to create directory [%s] for extracting bundled files."), path.c_str());
        throw StatusCode::BundleExtractionIOError;
    }
}

void dir_utils_t::remove_directory_tree(const pal::string_t& path)
{
    if (path.empty())
    {
        return;
    }

    std::vector<pal::string_t> dirs;
    pal::readdir_onlydirectories(path, &dirs);

    for (const pal::string_t &dir : dirs)
    {
        remove_directory_tree(dir);
    }

    std::vector<pal::string_t> files;
    pal::readdir(path, &files);

    for (const pal::string_t &file : files)
    {
        if (!pal::remove(file.c_str()))
        {
            trace::warning(_X("Failed to remove temporary file [%s]."), file.c_str());
        }
    }

    if (!pal::rmdir(path.c_str()))
    {
        trace::warning(_X("Failed to remove temporary directory [%s]."), path.c_str());
    }
}

// Fixup a path to have current platform's directory separator.
void dir_utils_t::fixup_path_separator(pal::string_t& path)
{
    const pal::char_t bundle_dir_separator = '/';

    if (bundle_dir_separator != DIR_SEPARATOR)
    {
        for (size_t pos = path.find(bundle_dir_separator);
            pos != pal::string_t::npos;
            pos = path.find(bundle_dir_separator, pos))
        {
            path[pos] = DIR_SEPARATOR;
        }
    }
}

// Retry the rename operation with some wait in between the attempts.
// This is an attempt to workaround for possible file locking caused by AV software.
bool dir_utils_t::rename_with_retries(pal::string_t& old_name, pal::string_t& new_name, bool (*intermediate_check)())
{
    for(int retry_count; retry_count < 500; retry_count++)
    {
        if (pal::rename(old_name.c_str(), new_name.c_str()) == 0)
        {
            return true;
        }
        bool should_retry = errno == EACCES;

        if (intermediate_check != nullptr && intermediate_check())
        {
            return true;
        }

        if (should_retry)
        {
            trace::info(_X("Retrying Rename [%s] to [%s] due to EACCES error"), old_name.c_str(), new_name.c_str());
            pal::sleep(100);
            continue;
        }
        else
        {
            return false;
        }
    }

    return false;
}

