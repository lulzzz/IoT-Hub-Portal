﻿// Copyright (c) CGI France. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AzureIoTHub.Portal.Server.Controllers.V10
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Data.Tables;
    using AzureIoTHub.Portal.Server.Factories;
    using AzureIoTHub.Portal.Server.Helpers;
    using AzureIoTHub.Portal.Server.Managers;
    using AzureIoTHub.Portal.Server.Services;
    using AzureIoTHub.Portal.Shared.Models.V10.DeviceModel;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    public abstract class DeviceModelsControllerBase<TModel> : ControllerBase
        where TModel: DeviceModel
    {
        /// <summary>
        /// The default partition key.
        /// </summary>
        public const string DefaultPartitionKey = "0";

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<DeviceModelsControllerBase<TModel>> log;

        /// <summary>
        /// The table client factory.
        /// </summary>
        private readonly ITableClientFactory tableClientFactory;

        /// <summary>
        /// The device model mapper.
        /// </summary>
        private readonly IDeviceModelMapper<TModel> deviceModelMapper;

        /// <summary>
        /// The device model image manager.
        /// </summary>
        private readonly IDeviceModelImageManager deviceModelImageManager;

        /// <summary>
        /// The devices service.
        /// </summary>
        private readonly IDeviceService devicesService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceModelsController"/> class.
        /// </summary>
        /// <param name="log">The logger.</param>
        /// <param name="deviceModelImageManager">The device model image manager.</param>
        /// <param name="deviceModelMapper">The device model mapper.</param>
        /// <param name="devicesService">The devices service.</param>
        /// <param name="tableClientFactory">The table client factory.</param>
        public DeviceModelsControllerBase(
            ILogger<DeviceModelsControllerBase<TModel>> log,
            IDeviceModelImageManager deviceModelImageManager,
            IDeviceModelMapper<TModel> deviceModelMapper,
            IDeviceService devicesService,
            ITableClientFactory tableClientFactory)
        {
            this.log = log;
            this.deviceModelMapper = deviceModelMapper;
            this.tableClientFactory = tableClientFactory;
            this.deviceModelImageManager = deviceModelImageManager;
            this.devicesService = devicesService;
        }

        /// <summary>
        /// Gets the device models.
        /// </summary>
        /// <returns>The list of device models.</returns>
        [HttpGet()]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            // PartitionKey 0 contains all device models
            var entities = this.tableClientFactory
                            .GetDeviceTemplates()
                            .Query<TableEntity>();

            // Converts the query result into a list of device models
            var deviceModelList = entities.Select(this.deviceModelMapper.CreateDeviceModel);

            return this.Ok(deviceModelList.ToArray());
        }

        /// <summary>
        /// Gets the specified model identifier.
        /// </summary>
        /// <param name="id">The model identifier.</param>
        /// <returns>The corresponding model.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Get(string id)
        {
            try
            {
                var query = this.tableClientFactory
                            .GetDeviceTemplates()
                            .GetEntity<TableEntity>(DefaultPartitionKey, id);

                return this.Ok(this.deviceModelMapper.CreateDeviceModel(query.Value));
            }
            catch (RequestFailedException e)
            {
                if (e.Status == StatusCodes.Status404NotFound)
                {
                    return this.NotFound();
                }

                this.log.LogError(e.Message, e);

                throw;
            }
        }

        /// <summary>
        /// Gets the avatar.
        /// </summary>
        /// <param name="id">The model identifier.</param>
        /// <returns>The avatar.</returns>
        [HttpGet("{id}/avatar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetAvatar(string id)
        {
            try
            {
                var query = this.tableClientFactory
                            .GetDeviceTemplates()
                            .GetEntity<TableEntity>(DefaultPartitionKey, id);

                return this.Ok(this.deviceModelImageManager.ComputeImageUri(id));
            }
            catch (RequestFailedException e)
            {
                if (e.Status == StatusCodes.Status404NotFound)
                {
                    return this.NotFound();
                }

                this.log.LogError(e.Message, e);

                throw;
            }
        }

        /// <summary>
        /// Changes the avatar.
        /// </summary>
        /// <param name="id">The model identifier.</param>
        /// <param name="file">The file.</param>
        /// <returns>The avatar.</returns>
        [HttpPost("{id}/avatar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeAvatar(string id, IFormFile file)
        {
            try
            {
                var query = this.tableClientFactory
                               .GetDeviceTemplates()
                               .GetEntity<TableEntity>(DefaultPartitionKey, id);

                return this.Ok(await this.deviceModelImageManager.ChangeDeviceModelImageAsync(id, file.OpenReadStream()));
            }
            catch (RequestFailedException e)
            {
                if (e.Status == StatusCodes.Status404NotFound)
                {
                    return this.NotFound();
                }

                this.log.LogError(e.Message, e);

                throw;
            }
        }

        /// <summary>
        /// Deletes the avatar.
        /// </summary>
        /// <param name="id">The model identifier.</param>
        [HttpDelete("{id}/avatar")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAvatar(string id)
        {
            try
            {
                var query = this.tableClientFactory
                               .GetDeviceTemplates()
                               .GetEntity<TableEntity>(DefaultPartitionKey, id);

                await this.deviceModelImageManager.DeleteDeviceModelImageAsync(id);

                return this.NoContent();
            }
            catch (RequestFailedException e)
            {
                if (e.Status == StatusCodes.Status404NotFound)
                {
                    return this.NotFound();
                }

                this.log.LogError(e.Message, e);

                throw;
            }
        }

        /// <summary>
        /// Creates the specified device model.
        /// </summary>
        /// <param name="deviceModel">The device model.</param>
        /// <returns>The action result.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post(TModel deviceModel)
        {
            if (!string.IsNullOrEmpty(deviceModel.ModelId))
            {
                try
                {
                    var query = this.tableClientFactory
                                   .GetDeviceTemplates()
                                   .GetEntity<TableEntity>(DefaultPartitionKey, deviceModel.ModelId);

                    return this.BadRequest("Cannot create a device model with an existing id.");
                }
                catch (RequestFailedException e)
                {
                    if (e.Status != StatusCodes.Status404NotFound)
                    {
                        this.log.LogError(e.Message, e);

                        throw;
                    }
                }
            }

            TableEntity entity = new TableEntity()
            {
                PartitionKey = DefaultPartitionKey,
                RowKey = deviceModel.ModelId ?? Guid.NewGuid().ToString()
            };

            await this.SaveEntity(entity, deviceModel);

            return this.Ok();
        }

        /// <summary>
        /// Updates the specified device model.
        /// </summary>
        /// <param name="deviceModel">The device model.</param>
        /// <returns>The action result.</returns>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Put(TModel deviceModel)
        {
            if (string.IsNullOrEmpty(deviceModel.ModelId))
            {
                return this.BadRequest("Should provide the model id.");
            }

            TableEntity entity;

            try
            {
                entity = this.tableClientFactory
                               .GetDeviceTemplates()
                               .GetEntity<TableEntity>(DefaultPartitionKey, deviceModel.ModelId);
            }
            catch (RequestFailedException e)
            {
                if (e.Status == StatusCodes.Status404NotFound)
                {
                    return this.NotFound();
                }

                this.log.LogError(e.Message, e);

                throw;
            }

            await this.SaveEntity(entity, deviceModel);

            return this.Ok();
        }

        /// <summary>
        /// Deletes the specified device model.
        /// </summary>
        /// <param name="id">The device model identifier.</param>
        /// <returns>The action result.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            // we get all devices
            var deviceList = await this.devicesService.GetAllDevice();
            TableEntity entity = null;

            try
            {
                entity = this.tableClientFactory
                            .GetDeviceTemplates()
                            .GetEntity<TableEntity>(DefaultPartitionKey, id);
            }
            catch (RequestFailedException e)
            {
                if (e.Status == StatusCodes.Status404NotFound)
                {
                    return this.NotFound();
                }

                this.log.LogError(e.Message, e);

                throw;
            }

            if (deviceList.Any(x => DeviceHelper.RetrieveTagValue(x, "modelId") == id))
            {
                return this.BadRequest("This model is already in use by a device and cannot be deleted.");
            }

            var queryCommand = this.tableClientFactory
                                   .GetDeviceCommands()
                                   .Query<TableEntity>(t => t.PartitionKey == id)
                                   .ToArray();

            foreach (var item in queryCommand)
            {
                _ = await this.tableClientFactory
                                .GetDeviceCommands()
                                .DeleteEntityAsync(id, item.RowKey);
            }

            // Image deletion
            await this.deviceModelImageManager.DeleteDeviceModelImageAsync(id);

            var result = await this.tableClientFactory
                .GetDeviceTemplates()
                .DeleteEntityAsync(DefaultPartitionKey, id);

            return this.NoContent();
        }

        /// <summary>
        /// Saves the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="deviceModelObject">The device model object.</param>
        private async Task SaveEntity(TableEntity entity, TModel deviceModelObject)
        {
            this.deviceModelMapper.UpdateTableEntity(entity, deviceModelObject);

            await this.tableClientFactory
                .GetDeviceTemplates()
                .UpsertEntityAsync(entity);
        }
    }
}