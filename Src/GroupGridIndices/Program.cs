﻿// -----------------------------------------------------------------------------
//                                ILGPU Samples
//                 Copyright (c) 2017-2019 ILGPU Samples Project
//                                www.ilgpu.net
//
// File: Program.cs
//
// This file is part of ILGPU and is distributed under the University of
// Illinois Open Source License. See LICENSE.txt for details.
// -----------------------------------------------------------------------------

using ILGPU;
using ILGPU.Runtime;
using System;

namespace GroupGridIndices
{
    class Program
    {
        /// <summary>
        /// Resolves the current thread index within a group.
        /// </summary>
        /// <returns>The current thread index within a group.</returns>
        static int GetGroupIndex()
        {
            // Gets the current thread index within a group.
            // Alternative: Group.Index.X
            return Group.IdxX;
        }

        /// <summary>
        /// Resolves the current thread-group index within a grid.
        /// </summary>
        /// <returns>The current thread-group index within a grid.</returns>
        static int GetGridIndex()
        {
            // Gets the current group index within the grid.
            // Alternative: Grid.Index.X
            return Grid.IdxX;
        }

        /// <summary>
        /// Writes data to the globally unique thread index using static properties.
        /// </summary>
        /// <param name="dataView">The target view.</param>
        /// <param name="constant">The constant to write into the data view.</param>
        static void WriteToGlobalIndex(ArrayView<int> dataView, int constant)
        {
            var globalIndex = Group.DimX * GetGridIndex() + GetGroupIndex();

            if (globalIndex < dataView.Length)
                dataView[globalIndex] = constant;
        }

        /// <summary>
        /// A grouped kernel that uses a helper function
        /// </summary>
        /// <param name="dataView">The target view.</param>
        /// <param name="constant">A uniform constant.</param>
        static void GroupedKernel(
            ArrayView<int> dataView,     // A view to a chunk of memory (1D in this case)
            int constant)                // A sample uniform constant
        {
            WriteToGlobalIndex(dataView, constant);
        }

        /// <summary>
        /// Demonstrates kernels using static properties to access grid and group indices.
        /// </summary>
        static void Main()
        {
            // Create main context
            using (var context = new Context())
            {
                // For each available accelerator...
                foreach (var acceleratorId in Accelerator.Accelerators)
                {
                    // Create default accelerator for the given accelerator id
                    using (var accelerator = Accelerator.Create(context, acceleratorId))
                    {
                        Console.WriteLine($"Performing operations on {accelerator}");

                        var groupSize = accelerator.MaxNumThreadsPerGroup;
                        KernelConfig kernelConfig = (2, groupSize);

                        using (var buffer = accelerator.Allocate<int>(kernelConfig.Size))
                        {
                            var groupedKernel = accelerator.LoadStreamKernel<ArrayView<int>, int>(GroupedKernel);
                            groupedKernel(kernelConfig, buffer.View, 64);

                            accelerator.Synchronize();

                            Console.WriteLine("Default grouped kernel");
                            var data = buffer.GetAsArray();
                            for (int i = 0, e = data.Length; i < e; ++i)
                                Console.WriteLine($"Data[{i}] = {data[i]}");
                        }
                    }
                }
            }
        }
    }
}
