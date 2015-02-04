﻿
using System;
using System.Data.Common;
using Google.Apis.Auth.OAuth2.Flows;

namespace CS498.Lib
{
    public class GoogleEvent
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public TimeBlock TimeBlock { get; set; }
        public string Id { get; private set; }

        public GoogleEvent(string id)
        {
            Initialize(id);
        }

        public GoogleEvent()
        {
            Initialize(Guid.NewGuid().ToString().Replace('-','a'));
        }

        private void Initialize(string id)
        {
            Id = id;
        }
    }
}