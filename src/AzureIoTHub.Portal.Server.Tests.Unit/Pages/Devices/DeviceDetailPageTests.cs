// Copyright (c) CGI France. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AzureIoTHub.Portal.Server.Tests.Unit.Pages.Devices
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AzureIoTHub.Portal.Client.Pages.Devices;
    using Models.v10;
    using Bunit;
    using Bunit.TestDoubles;
    using Client.Exceptions;
    using Client.Models;
    using Client.Services;
    using FluentAssertions;
    using FluentAssertions.Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using Mocks;
    using Moq;
    using MudBlazor;
    using MudBlazor.Services;
    using NUnit.Framework;

    [TestFixture]
    public class DeviceDetailPageTests : BlazorUnitTest
    {
        private Mock<IDialogService> mockDialogService;
        private Mock<ISnackbar> mockSnackbarService;
        private FakeNavigationManager mockNavigationManager;

        private Mock<IDeviceModelsClientService> mockDeviceModelsClientService;
        private Mock<ILoRaWanDeviceModelsClientService> mockLoRaWanDeviceModelsClientService;
        private Mock<IDeviceTagSettingsClientService> mockDeviceTagSettingsClientService;
        private Mock<IDeviceClientService> mockDeviceClientService;
        private Mock<ILoRaWanDeviceClientService> mockLoRaWanDeviceClientService;

        public override void Setup()
        {
            base.Setup();

            this.mockDialogService = MockRepository.Create<IDialogService>();
            this.mockSnackbarService = MockRepository.Create<ISnackbar>();
            this.mockDeviceModelsClientService = MockRepository.Create<IDeviceModelsClientService>();
            this.mockLoRaWanDeviceModelsClientService = MockRepository.Create<ILoRaWanDeviceModelsClientService>();
            this.mockDeviceTagSettingsClientService = MockRepository.Create<IDeviceTagSettingsClientService>();
            this.mockDeviceClientService = MockRepository.Create<IDeviceClientService>();
            this.mockLoRaWanDeviceClientService = MockRepository.Create<ILoRaWanDeviceClientService>();

            _ = Services.AddSingleton(this.mockDialogService.Object);
            _ = Services.AddSingleton(this.mockSnackbarService.Object);
            _ = Services.AddSingleton(this.mockDeviceModelsClientService.Object);
            _ = Services.AddSingleton(this.mockLoRaWanDeviceModelsClientService.Object);
            _ = Services.AddSingleton(this.mockDeviceTagSettingsClientService.Object);
            _ = Services.AddSingleton(this.mockDeviceClientService.Object);
            _ = Services.AddSingleton(this.mockLoRaWanDeviceClientService.Object);

            _ = Services.AddSingleton<IDeviceLayoutService, DeviceLayoutService>();

            _ = Services.AddSingleton(new PortalSettings { IsLoRaSupported = false });

            Services.Add(new ServiceDescriptor(typeof(IResizeObserver), new MockResizeObserver()));

            this.mockNavigationManager = Services.GetRequiredService<FakeNavigationManager>();
        }

        [Test]
        public void ReturnButtonMustNavigateToPreviousPage()
        {

            // Arrange
            var deviceId = Guid.NewGuid().ToString();
            var modelId = Guid.NewGuid().ToString();

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDevice(deviceId))
                .ReturnsAsync(new DeviceDetails() { ModelId = modelId });

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDeviceProperties(deviceId))
                .ReturnsAsync(new List<DevicePropertyValue>());

            _ = this.mockDeviceModelsClientService.Setup(service => service.GetDeviceModel(modelId))
                .ReturnsAsync(new DeviceModel());

            _ = this.mockDeviceTagSettingsClientService.Setup(service => service.GetDeviceTags())
                .ReturnsAsync(new List<DeviceTag>());


            // Act
            var cut = RenderComponent<DeviceDetailPage>(ComponentParameter.CreateParameter("DeviceID", deviceId));
            var returnButton = cut.WaitForElement("#returnButton");

            returnButton.Click();

            // Assert
            cut.WaitForAssertion(() => this.mockNavigationManager.Uri.Should().EndWith("/devices"));
            cut.WaitForAssertion(() => MockRepository.VerifyAll());
        }

        [Test]
        public void ClickOnSaveShouldPutDeviceDetails()
        {
            var mockDeviceModel = new DeviceModel
            {
                ModelId = Guid.NewGuid().ToString(),
                Description = Guid.NewGuid().ToString(),
                SupportLoRaFeatures = false,
                Name = Guid.NewGuid().ToString()
            };

            var mockTag = new DeviceTag
            {
                Label = Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString(),
                Required = false,
                Searchable = false
            };

            var mockDeviceDetails = new DeviceDetails
            {
                DeviceName = Guid.NewGuid().ToString(),
                ModelId = mockDeviceModel.ModelId,
                DeviceID = Guid.NewGuid().ToString(),
                Tags = new Dictionary<string, string>()
                {
                    {mockTag.Name,Guid.NewGuid().ToString()}
                }
            };

            _ = this.mockDeviceClientService.Setup(service => service.UpdateDevice(It.Is<DeviceDetails>(details => mockDeviceDetails.DeviceID.Equals(details.DeviceID, StringComparison.Ordinal))))
                .Returns(Task.CompletedTask);

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDevice(mockDeviceDetails.DeviceID))
                .ReturnsAsync(mockDeviceDetails);

            _ = this.mockDeviceModelsClientService.Setup(service => service.GetDeviceModel(mockDeviceDetails.ModelId))
                .ReturnsAsync(mockDeviceModel);

            _ = this.mockDeviceTagSettingsClientService.Setup(service => service.GetDeviceTags())
                .ReturnsAsync(new List<DeviceTag>
                {
                    mockTag
                });

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDeviceProperties(mockDeviceDetails.DeviceID))
                .ReturnsAsync(new List<DevicePropertyValue>());

            _ = this.mockDeviceClientService
                .Setup(service => service.SetDeviceProperties(mockDeviceDetails.DeviceID, It.IsAny<IList<DevicePropertyValue>>()))
                .Returns(Task.CompletedTask);

            _ = this.mockSnackbarService.Setup(c => c.Add(It.IsAny<string>(), Severity.Success, null)).Returns((Snackbar)null);

            // Act
            var cut = RenderComponent<DeviceDetailPage>(ComponentParameter.CreateParameter("DeviceID", mockDeviceDetails.DeviceID));
            cut.WaitForAssertion(() => cut.Find($"#{nameof(DeviceModel.Name)}>b").InnerHtml.Should().NotBeEmpty());

            var saveButton = cut.WaitForElement("#saveButton");
            saveButton.Click();

            // Assert
            cut.WaitForState(() => this.mockNavigationManager.Uri.EndsWith("devices", StringComparison.OrdinalIgnoreCase), 3.Seconds());
            cut.WaitForAssertion(() => MockRepository.VerifyAll());
        }

        [Test]
        public void SaveShouldProcessProblemDetailsExceptionWhenIssueOccursOnUpdatingDevice()
        {
            var mockDeviceModel = new DeviceModel
            {
                ModelId = Guid.NewGuid().ToString(),
                Description = Guid.NewGuid().ToString(),
                SupportLoRaFeatures = false,
                Name = Guid.NewGuid().ToString()
            };

            var mockTag = new DeviceTag
            {
                Label = Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString(),
                Required = false,
                Searchable = false
            };

            var mockDeviceDetails = new DeviceDetails
            {
                DeviceName = Guid.NewGuid().ToString(),
                ModelId = mockDeviceModel.ModelId,
                DeviceID = Guid.NewGuid().ToString(),
                Tags = new Dictionary<string, string>()
                {
                    {mockTag.Name,Guid.NewGuid().ToString()}
                }
            };

            _ = this.mockDeviceClientService.Setup(service => service.UpdateDevice(It.Is<DeviceDetails>(details => mockDeviceDetails.DeviceID.Equals(details.DeviceID, StringComparison.Ordinal))))
                .ThrowsAsync(new ProblemDetailsException(new ProblemDetailsWithExceptionDetails()));

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDevice(mockDeviceDetails.DeviceID))
                .ReturnsAsync(mockDeviceDetails);

            _ = this.mockDeviceModelsClientService.Setup(service => service.GetDeviceModel(mockDeviceDetails.ModelId))
                .ReturnsAsync(mockDeviceModel);

            _ = this.mockDeviceTagSettingsClientService.Setup(service => service.GetDeviceTags())
                .ReturnsAsync(new List<DeviceTag>
                {
                    mockTag
                });

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDeviceProperties(mockDeviceDetails.DeviceID))
                .ReturnsAsync(new List<DevicePropertyValue>());

            // Act
            var cut = RenderComponent<DeviceDetailPage>(ComponentParameter.CreateParameter("DeviceID", mockDeviceDetails.DeviceID));

            var saveButton = cut.WaitForElement("#saveButton");
            saveButton.Click();

            // Assert
            cut.WaitForAssertion(() => this.mockNavigationManager.Uri.Should().NotEndWith("devices"));
            cut.WaitForAssertion(() => MockRepository.VerifyAll());
        }

        [Test]
        public void ClickOnSaveShouldDisplaySnackbarIfValidationError()
        {
            var mockDeviceModel = new DeviceModel
            {
                ModelId = Guid.NewGuid().ToString(),
                Description = Guid.NewGuid().ToString(),
                SupportLoRaFeatures = false,
                Name = Guid.NewGuid().ToString()
            };

            var mockTag = new DeviceTag
            {
                Label = Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString(),
                Required = false,
                Searchable = false
            };

            var mockDeviceDetails = new DeviceDetails
            {
                DeviceName = Guid.NewGuid().ToString(),
                ModelId = mockDeviceModel.ModelId,
                DeviceID = Guid.NewGuid().ToString(),
                Tags = new Dictionary<string, string>()
                {
                    {mockTag.Name,Guid.NewGuid().ToString()}
                }
            };

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDevice(mockDeviceDetails.DeviceID))
                .ReturnsAsync(mockDeviceDetails);

            _ = this.mockDeviceModelsClientService.Setup(service => service.GetDeviceModel(mockDeviceDetails.ModelId))
                .ReturnsAsync(mockDeviceModel);

            _ = this.mockDeviceTagSettingsClientService.Setup(service => service.GetDeviceTags())
                .ReturnsAsync(new List<DeviceTag>
                {
                    mockTag
                });

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDeviceProperties(mockDeviceDetails.DeviceID))
                .ReturnsAsync(new List<DevicePropertyValue>());

            _ = this.mockSnackbarService.Setup(c => c.Add(It.IsAny<string>(), Severity.Error, null)).Returns((Snackbar)null);

            // Act
            var cut = RenderComponent<DeviceDetailPage>(ComponentParameter.CreateParameter("DeviceID", mockDeviceDetails.DeviceID));

            cut.WaitForElement($"#{nameof(DeviceDetails.DeviceName)}").Change("");
            var saveButton = cut.WaitForElement("#saveButton");
            saveButton.Click();

            // Assert
            cut.WaitForAssertion(() => MockRepository.VerifyAll());
        }

        [Test]
        public void ClickOnConnectShouldDisplayDeviceCredentials()
        {
            var mockDeviceModel = new DeviceModel
            {
                ModelId = Guid.NewGuid().ToString(),
                Description = Guid.NewGuid().ToString(),
                SupportLoRaFeatures = false,
                Name = Guid.NewGuid().ToString()
            };

            var mockTag = new DeviceTag
            {
                Label = Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString(),
                Required = false,
                Searchable = false
            };

            var mockDeviceDetails = new DeviceDetails
            {
                DeviceName = Guid.NewGuid().ToString(),
                ModelId = mockDeviceModel.ModelId,
                DeviceID = Guid.NewGuid().ToString(),
                Tags = new Dictionary<string, string>()
                {
                    {mockTag.Name,Guid.NewGuid().ToString()}
                }
            };

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDevice(mockDeviceDetails.DeviceID))
                .ReturnsAsync(mockDeviceDetails);

            _ = this.mockDeviceModelsClientService.Setup(service => service.GetDeviceModel(mockDeviceDetails.ModelId))
                .ReturnsAsync(mockDeviceModel);

            _ = this.mockDeviceTagSettingsClientService.Setup(service => service.GetDeviceTags())
                .ReturnsAsync(new List<DeviceTag>
                {
                    mockTag
                });

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDeviceProperties(mockDeviceDetails.DeviceID))
                .ReturnsAsync(new List<DevicePropertyValue>());

            var mockDialogReference = new DialogReference(Guid.NewGuid(), this.mockDialogService.Object);
            _ = this.mockDialogService.Setup(c => c.Show<ConnectionStringDialog>(It.IsAny<string>(), It.IsAny<DialogParameters>()))
                .Returns(mockDialogReference);

            // Act
            var cut = RenderComponent<DeviceDetailPage>(ComponentParameter.CreateParameter("DeviceID", mockDeviceDetails.DeviceID));

            var connectButton = cut.WaitForElement("#connectButton");
            connectButton.Click();

            // Assert
            cut.WaitForAssertion(() => MockRepository.VerifyAll());
        }

        [Test]
        public void ClickOnDeleteShouldDisplayConfirmationDialogAndReturnIfAborted()
        {
            var mockDeviceModel = new DeviceModel
            {
                ModelId = Guid.NewGuid().ToString(),
                Description = Guid.NewGuid().ToString(),
                SupportLoRaFeatures = false,
                Name = Guid.NewGuid().ToString()
            };

            var mockTag = new DeviceTag
            {
                Label = Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString(),
                Required = false,
                Searchable = false
            };

            var mockDeviceDetails = new DeviceDetails
            {
                DeviceName = Guid.NewGuid().ToString(),
                ModelId = mockDeviceModel.ModelId,
                DeviceID = Guid.NewGuid().ToString(),
                Tags = new Dictionary<string, string>()
                {
                    {mockTag.Name,Guid.NewGuid().ToString()}
                }
            };

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDevice(mockDeviceDetails.DeviceID))
                .ReturnsAsync(mockDeviceDetails);

            _ = this.mockDeviceModelsClientService.Setup(service => service.GetDeviceModel(mockDeviceDetails.ModelId))
                .ReturnsAsync(mockDeviceModel);

            _ = this.mockDeviceTagSettingsClientService.Setup(service => service.GetDeviceTags())
                .ReturnsAsync(new List<DeviceTag>
                {
                    mockTag
                });

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDeviceProperties(mockDeviceDetails.DeviceID))
                .ReturnsAsync(new List<DevicePropertyValue>());

            var mockDialogReference = MockRepository.Create<IDialogReference>();
            _ = mockDialogReference.Setup(c => c.Result).ReturnsAsync(DialogResult.Cancel());
            _ = this.mockDialogService.Setup(c => c.Show<DeleteDevicePage>(It.IsAny<string>(), It.IsAny<DialogParameters>()))
                .Returns(mockDialogReference.Object);

            // Act
            var cut = RenderComponent<DeviceDetailPage>(ComponentParameter.CreateParameter("DeviceID", mockDeviceDetails.DeviceID));

            var deleteButton = cut.WaitForElement("#deleteButton");
            deleteButton.Click();

            // Assert
            cut.WaitForAssertion(() => MockRepository.VerifyAll());
        }

        [Test]
        public void ClickOnDeleteShouldDisplayConfirmationDialogAndRedirectIfConfirmed()
        {
            var mockDeviceModel = new DeviceModel
            {
                ModelId = Guid.NewGuid().ToString(),
                Description = Guid.NewGuid().ToString(),
                SupportLoRaFeatures = false,
                Name = Guid.NewGuid().ToString()
            };

            var mockTag = new DeviceTag
            {
                Label = Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString(),
                Required = false,
                Searchable = false
            };

            var mockDeviceDetails = new DeviceDetails
            {
                DeviceName = Guid.NewGuid().ToString(),
                ModelId = mockDeviceModel.ModelId,
                DeviceID = Guid.NewGuid().ToString(),
                Tags = new Dictionary<string, string>()
                {
                    {mockTag.Name,Guid.NewGuid().ToString()}
                }
            };

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDevice(mockDeviceDetails.DeviceID))
                .ReturnsAsync(mockDeviceDetails);

            _ = this.mockDeviceModelsClientService.Setup(service => service.GetDeviceModel(mockDeviceDetails.ModelId))
                .ReturnsAsync(mockDeviceModel);

            _ = this.mockDeviceTagSettingsClientService.Setup(service => service.GetDeviceTags())
                .ReturnsAsync(new List<DeviceTag>
                {
                    mockTag
                });

            _ = this.mockDeviceClientService
                .Setup(service => service.GetDeviceProperties(mockDeviceDetails.DeviceID))
                .ReturnsAsync(new List<DevicePropertyValue>());

            var mockDialogReference = MockRepository.Create<IDialogReference>();
            _ = mockDialogReference.Setup(c => c.Result).ReturnsAsync(DialogResult.Ok("Ok"));
            _ = this.mockDialogService.Setup(c => c.Show<DeleteDevicePage>(It.IsAny<string>(), It.IsAny<DialogParameters>()))
                .Returns(mockDialogReference.Object);

            // Act
            var cut = RenderComponent<DeviceDetailPage>(ComponentParameter.CreateParameter("DeviceID", mockDeviceDetails.DeviceID));

            var deleteButton = cut.WaitForElement("#deleteButton");
            deleteButton.Click();

            // Assert            
            cut.WaitForState(() => this.mockNavigationManager.Uri.EndsWith("/devices", StringComparison.OrdinalIgnoreCase));
            cut.WaitForAssertion(() => MockRepository.VerifyAll());
        }
    }
}
