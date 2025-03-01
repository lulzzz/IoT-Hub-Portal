// Copyright (c) CGI France. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AzureIoTHub.Portal.Server.Services
{
    using System.Threading.Tasks;
    using AzureIoTHub.Portal.Models.v10.LoRaWAN;

    public interface ILoRaWANCommandService
    {
        Task<DeviceModelCommand[]> GetDeviceModelCommandsFromModel(string id);
        Task PostDeviceModelCommands(string id, DeviceModelCommand[] commands);
    }
}
