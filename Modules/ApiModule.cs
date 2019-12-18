using System.IO;
using System.Collections;
using System;
using System.Collections.Generic;
using Nancy.ModelBinding;
using StorylineBackend.models;
using StorylineBackend.upload;
using Nancy;
using Newtonsoft.Json;

namespace StorylineBackend.modules
{


    public class ApiModule : NancyModule
    {
        private IFileUploadHandler _fileUploadHandler;
        private ILayoutHandler _layoutHandler;
        public ApiModule(IFileUploadHandler fileUploadHandler, ILayoutHandler layoutHandler): base("/api")
        {
            _fileUploadHandler = fileUploadHandler;
            _layoutHandler = layoutHandler;
            Post("/upload", async (args, ctx) =>
            {
                var request = this.Bind<UploadRequest>();
                var uploadResult = await _fileUploadHandler.HandleUpload(request.File.Value);
                var response = new ApiResponse<FileUploadResult>(ApiResponse<FileUploadResult>.OK, uploadResult);

                return Negotiate
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithModel(response);
            });

            Get("/layout", async (args, ctx) =>
            {
                var request = Request.Query["id"].ToString();
                var result = await _layoutHandler.handleLayout(request);
                var code = result == null ? ApiResponse<LayoutResult>.ERR : ApiResponse<LayoutResult>.OK;
                var status = result == null ? HttpStatusCode.BadRequest : HttpStatusCode.OK;
                var response = new ApiResponse<LayoutResult>(code, result);
                
                return Negotiate
                    .WithStatusCode(status)
                    .WithModel(response);
            });

            Post("/update", async (args, ctx) =>
            {
                var request = this.Bind<UpdateRequest>();
                var result = await _layoutHandler.updateLayout(request);
                var response = new ApiResponse<LayoutResult>(ApiResponse<LayoutResult>.OK, result);
                return Negotiate
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithModel(response);
            });
        }
    }
}