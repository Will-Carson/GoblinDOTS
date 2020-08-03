// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System.Runtime.CompilerServices;

    static class MathUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOutOfBounds(int index, int length, int capacity) =>
            (index | length | (index + length) | (capacity - (index + length))) < 0;
    }
}
