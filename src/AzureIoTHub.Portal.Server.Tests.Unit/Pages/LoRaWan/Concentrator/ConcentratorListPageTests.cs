// Copyright (c) CGI France. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AzureIoTHub.Portal.Server.Tests.Unit.Pages.LoRaWan.Concentrator
{
    using System;
    using System.Collections.Generic;
    using AzureIoTHub.Portal.Client.Pages.LoRaWAN.Concentrator;
    using Bunit;
    using Bunit.TestDoubles;
    using Client.Exceptions;
    using Client.Models;
    using Client.Services;
    using FluentAssertions;
    using Microsoft.Extensions.DependencyInjection;
    using Models.v10;
    using Models.v10.LoRaWAN;
    using Moq;
    using MudBlazor;
    using NUnit.Framework;

    [TestFixture]
    public class ConcentratorListPageTests : BlazorUnitTest
    {
        private Mock<IDialogService> mockDialogService;
        private Mock<ISnackbar> mockSnackbarService;
        private Mock<ILoRaWanConcentratorsClientService> mockLoRaWanConcentratorsClientService;

        public override void Setup()
        {
            base.Setup();

            this.mockDialogService = MockRepository.Create<IDialogService>();
            this.mockSnackbarService = MockRepository.Create<ISnackbar>();
            this.mockLoRaWanConcentratorsClientService = MockRepository.Create<ILoRaWanConcentratorsClientService>();

            _ = Services.AddSingleton(this.mockDialogService.Object);
            _ = Services.AddSingleton(this.mockSnackbarService.Object);
            _ = Services.AddSingleton(this.mockLoRaWanConcentratorsClientService.Object);

            _ = Services.AddSingleton(new PortalSettings { IsLoRaSupported = true });
        }

        [Test]
        public void ConcentratorListPageShouldLoadAndShowConcentrators()
        {
            // Arrange
            var expectedUri = "api/lorawan/concentrators?pageSize=10";

            _ = this.mockLoRaWanConcentratorsClientService.Setup(service =>
                    service.GetConcentrators(It.Is<string>(s => expectedUri.Equals(s, StringComparison.Ordinal))))
                .ReturnsAsync(new PaginationResult<Concentrator>
                {
                    Items = new List<Concentrator>
                    {
                        new(),
                        new(),
                        new()
                    }
                });

            // Act
            var cut = RenderComponent<ConcentratorListPage>();
            cut.WaitForAssertion(() => cut.Find("#concentrators-listing"));
            cut.WaitForAssertion(() => cut.Markup.Should().NotContain("Loading..."));

            // Assert
            _ = cut.FindAll("tr").Count.Should().Be(4);
            cut.WaitForAssertion(() => MockRepository.VerifyAll());
        }

        [Test]
        public void ConcentratorListPageShouldProcessProblemDetailsExceptionWhenIssueOccursOnLoadingConcentrators()
        {
            // Arrange
            var expectedUri = "api/lorawan/concentrators?pageSize=10";

            _ = this.mockLoRaWanConcentratorsClientService.Setup(service =>
                    service.GetConcentrators(It.Is<string>(s => expectedUri.Equals(s, StringComparison.Ordinal))))
                .ThrowsAsync(new ProblemDetailsException(new ProblemDetailsWithExceptionDetails()));

            // Act
            var cut = RenderComponent<ConcentratorListPage>();
            cut.WaitForAssertion(() => cut.Find("#concentrators-listing"));
            cut.WaitForAssertion(() => cut.Markup.Should().NotContain("Loading..."));

            // Assert
            _ = cut.FindAll("tr").Count.Should().Be(2);
            MockHttpClient.VerifyNoOutstandingRequest();
            MockHttpClient.VerifyNoOutstandingExpectation();
        }

        [Test]
        public void ClickToItemShouldRedirectToConcentratorDetailsPage()
        {
            // Arrange
            var deviceId = Guid.NewGuid().ToString();
            var expectedUri = "api/lorawan/concentrators?pageSize=10";

            _ = this.mockLoRaWanConcentratorsClientService.Setup(service =>
                    service.GetConcentrators(It.Is<string>(s => expectedUri.Equals(s, StringComparison.Ordinal))))
                .ReturnsAsync(new PaginationResult<Concentrator>
                {
                    Items = new List<Concentrator>
                    {
                        new()
                        {
                            DeviceId = deviceId,
                        },
                        new()
                    }
                });

            var cut = RenderComponent<ConcentratorListPage>();
            cut.WaitForAssertion(() => cut.Markup.Should().NotContain("Loading..."));

            // Act
            cut.WaitForAssertion(() => cut.Find("table tbody tr").Click());

            // Assert
            cut.WaitForAssertion(() => Services.GetService<FakeNavigationManager>()?.Uri.Should().EndWith($"/lorawan/concentrators/{deviceId}"));
        }

        [Test]
        public void ClickOnAddNewDeviceShouldNavigateToNewDevicePage()
        {
            // Arrange
            const string expectedUri = "api/lorawan/concentrators?pageSize=10";

            _ = this.mockLoRaWanConcentratorsClientService.Setup(service =>
                    service.GetConcentrators(It.Is<string>(s => expectedUri.Equals(s, StringComparison.Ordinal))))
                .ReturnsAsync(new PaginationResult<Concentrator>
                {
                    Items = new List<Concentrator>
                    {
                        new(),
                        new(),
                        new()
                    }
                });

            // Act
            var cut = RenderComponent<ConcentratorListPage>();
            cut.WaitForAssertion(() => cut.Find("#concentrators-listing"));
            cut.WaitForAssertion(() => cut.Markup.Should().NotContain("Loading..."));
            cut.WaitForElement("#add-concentrator").Click();

            // Assert
            cut.WaitForAssertion(() => MockRepository.VerifyAll());
            cut.WaitForAssertion(() => Services.GetService<FakeNavigationManager>()?.Uri.Should().EndWith("/lorawan/concentrators/new"));
        }
    }
}
