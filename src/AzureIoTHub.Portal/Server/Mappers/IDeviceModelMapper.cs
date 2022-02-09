// Copyright (c) CGI France. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AzureIoTHub.Portal.Server.Managers
{
    using Azure.Data.Tables;
    using AzureIoTHub.Portal.Shared.Models.V10.DeviceModel;

    public interface IDeviceModelMapper<TModel>
        where TModel : DeviceModel
    {
        public TModel CreateDeviceModel(TableEntity entity);

        public void UpdateTableEntity(TableEntity entity, TModel model);
    }
}
