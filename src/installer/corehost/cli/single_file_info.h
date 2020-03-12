// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __SINGLE_FILE_INFO_H_
#define __SINGLE_FILE_INFO_H_

#include "pal.h"

struct single_file_info_t
{
    single_file_info_t(
        int64_t deps_json_offset_value,
        int64_t deps_json_size_value,
        int64_t runtimeconfig_json_offset_value,
        int64_t runtimeconfig_json_size_value,
        const pal::char_t* extraction_path_value)
      : deps_json_offset(deps_json_offset_value)
      , deps_json_size(deps_json_size_value)
      , runtimeconfig_json_offset(runtimeconfig_json_offset_value)
      , runtimeconfig_json_size(runtimeconfig_json_size_value)
      , extraction_path(extraction_path_value) {}

    const int64_t deps_json_offset;
    const int64_t deps_json_size;
    const int64_t runtimeconfig_json_offset;
    const int64_t runtimeconfig_json_size;
    const pal::char_t* extraction_path;
};

#endif // __SINGLE_FILE_INFO_H_
