// ----------------------------------------------------------------------
// <copyright file="GridColumn.cs" company="Xavier Solau">
// Copyright © 2021 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using System;

namespace SoloX.BlazorLayout.Containers.Grid
{
    /// <summary>
    /// A Column grid dimension.
    /// </summary>
    public class GridColumn : AGridDimension
    {
        ///<inheritdoc/>
        protected override void AddToGrid(GridContainer gridContainer)
        {
#pragma warning disable CA1510 // Use ArgumentNullException throw helper
            if (gridContainer == null)
            {
                throw new ArgumentNullException(nameof(gridContainer));
            }
#pragma warning restore CA1510 // Use ArgumentNullException throw helper

            gridContainer.Add(this);
        }
    }
}
