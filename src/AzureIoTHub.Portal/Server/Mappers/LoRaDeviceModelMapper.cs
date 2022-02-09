// Copyright (c) CGI France. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AzureIoTHub.Portal.Server.Mappers
{
    using Azure.Data.Tables;
    using AzureIoTHub.Portal.Server.Managers;
    using AzureIoTHub.Portal.Shared.Models.V10.LoRaWAN.LoRaDeviceModel;

    public class LoRaDeviceModelMapper : IDeviceModelMapper<LoRaDeviceModel>
    {
        private readonly IDeviceModelCommandsManager deviceModelCommandsManager;

        public LoRaDeviceModelMapper(IDeviceModelCommandsManager deviceModelCommandsManager)
        {
            this.deviceModelCommandsManager = deviceModelCommandsManager;
        }

        public LoRaDeviceModel CreateDeviceModel(TableEntity entity)
        {
            return new LoRaDeviceModel
            {
                ModelId = entity.RowKey,
                AppEUI = entity[nameof(LoRaDeviceModel.AppEUI)]?.ToString(),
                SensorDecoderURL = entity[nameof(LoRaDeviceModel.SensorDecoderURL)]?.ToString(),
                Commands = this.deviceModelCommandsManager.RetrieveDeviceModelCommands(entity.RowKey)
            };
        }

        public void UpdateTableEntity(TableEntity entity, LoRaDeviceModel model)
        {
            entity[nameof(LoRaDeviceModel.AppEUI)] = model.AppEUI;
            entity[nameof(LoRaDeviceModel.SensorDecoderURL)] = model.SensorDecoderURL;
        }
    }
}
