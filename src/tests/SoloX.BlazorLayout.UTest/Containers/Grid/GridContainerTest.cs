﻿// ----------------------------------------------------------------------
// <copyright file="GridContainerTest.cs" company="Xavier Solau">
// Copyright © 2021 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using SoloX.BlazorLayout.Containers.Grid;
using SoloX.BlazorLayout.UTest.Core;
using Xunit;

namespace SoloX.BlazorLayout.UTest.Containers.Grid
{
    public class GridContainerTest
    {
        [Fact]
        public void ItShouldRenderWithTheGivenId()
        {
            PanelHelpers.AssertIdIsProperlyRendered<GridContainer>();
        }

        [Fact]
        public void ItShouldRenderWithTheGivenClass()
        {
            PanelHelpers.AssertClassIsProperlyRendered<GridContainer>();
        }

        [Fact]
        public void ItShouldRenderWithAGridDisplay()
        {
            // Arrange
            using var ctx = new TestContext();

            // Act
            var cut = ctx.RenderComponent<GridContainer>(
                builder =>
                {
                    builder.AddChildContent<Column>();
                    builder.AddChildContent<Column>();
                    builder.AddChildContent<Row>();
                    builder.AddChildContent<Row>();
                });

            // Assert
            cut.Nodes.Length.Should().Be(1);
            var rootElement = cut.Nodes[0].As<IElement>();

            rootElement.LocalName.Should().Be(TagNames.Div);

            rootElement.ClassName.Should().Contain("grid-container");

            var style = rootElement.ComputeCurrentStyle();

            style.Should().ContainSingle(x => x.Name == "grid-template-columns" && x.Value == "1fr 1fr");
            style.Should().ContainSingle(x => x.Name == "grid-template-rows" && x.Value == "1fr 1fr");
        }

        [Fact]
        public void ItShouldRenderCellsAtTheRightPosition()
        {
            var cellId1 = "cell-id1";
            var cellId2 = "cell-id2";

            // Arrange
            using var ctx = new TestContext();

            // Act
            var cut = ctx.RenderComponent<GridContainer>(
                builder =>
                {
                    builder.AddChildContent<Column>();
                    builder.AddChildContent<Column>();
                    builder.AddChildContent<Row>();
                    builder.AddChildContent<Row>();
                    builder.AddChildContent<Cell>(
                        cellBuilder =>
                        {
                            cellBuilder.Add(c => c.Column, "0");
                            cellBuilder.Add(c => c.Row, "0");
                            cellBuilder.Add(c => c.Id, cellId1);
                        });
                    builder.AddChildContent<Cell>(
                        cellBuilder =>
                        {
                            cellBuilder.Add(c => c.Column, "1");
                            cellBuilder.Add(c => c.Row, "1");
                            cellBuilder.Add(c => c.Id, cellId2);
                        });
                });

            // Assert
            cut.Nodes.Length.Should().Be(1);

            var cellElt1 = cut.Find($"#{cellId1}");

            var attrStyle1 = cellElt1.GetAttribute("style");

            var style1 = ComputeCurrentStyle(attrStyle1);
            style1.Should().ContainSingle(x => x.Name == "grid-column" && x.Value == "1");
            style1.Should().ContainSingle(x => x.Name == "grid-row" && x.Value == "1");

            var cellElt2 = cut.Find($"#{cellId2}");
            var attrStyle2 = cellElt2.GetAttribute("style");

            var style2 = ComputeCurrentStyle(attrStyle2);

            style2.Should().ContainSingle(x => x.Name == "grid-column" && x.Value == "2");
            style2.Should().ContainSingle(x => x.Name == "grid-row" && x.Value == "2");
        }

        internal class CssProperty
        {
            public CssProperty(string name, string value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; }
            public string Value { get; }
        }

        internal static IEnumerable<CssProperty> ComputeCurrentStyle(string style)
        {
            return style.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(e =>
            {
                var words = e.Split(':');

                return new CssProperty(words[0].Trim(), words[1].Trim());
            });
        }
    }
}