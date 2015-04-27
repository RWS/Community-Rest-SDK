﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using Telligent.Evolution.Extensibility.Rest.Version1;
using Telligent.Evolution.Extensions.OAuthAuthentication.Implementations;
using Telligent.Evolution.Extensions.OAuthAuthentication.Services;
using Telligent.Evolution.RestSDK.Implementations;
using Telligent.Rest.SDK;
using Telligent.Rest.SDK.Implementation;
using Telligent.Rest.SDK.Model;

namespace Telligent.Evolution.RestSDK.Services
{
    public static class ServiceLocator
    {
		private static object _lockObject = new object();
		private static Dictionary<Type, object> _instances = null;

        public static T Get<T>()
        {
            EnsureInitialized();
			return (T)_instances[typeof(T)];
        }

        public static void EnsureInitialized(Dictionary<Type, object> instances = null)
        {
            if (instances != null)
            {
                lock (_lockObject)
                {
                    _instances = instances;
                }
            }
            else
            {
                if (_instances == null)
                    lock (_lockObject)
                        if (_instances == null)
                        {
                            HttpContextBase context = null;
                            if(HttpContext.Current != null)
                                context = new HttpContextWrapper(HttpContext.Current);

                            
                            var cache = new SimpleCache();
                            IConfigurationFile file;

                            if (context == null || System.Configuration.ConfigurationManager.AppSettings["communityServer:SDK:configPath"] != null)
                                file = new FileSystemConfigurationFile();
                            else
                                file = new WebConfigurationFile(context);

                            var configManager = new HostConfigurationManager(cache, file);

                            var localInstances = new Dictionary<Type, object>();
                         
                            var proxy = new RestCommunicationProxy();

                            var rest = new Telligent.Evolution.RestSDK.Implementations.Rest(proxy);
                            var deserializerService = new Deserializer();

                            localInstances[typeof(IRest)] = rest;
                            localInstances[typeof (IRestCache)] = cache;
                            localInstances[typeof(IDeserializer)] = deserializerService;
                            localInstances[typeof(IRestCommunicationProxy)] = proxy;

                            localInstances[typeof(IConfigurationFile)] = file;
                            localInstances[typeof(IHostConfigurationManager)] = configManager;

                            var userSyncService = new UserSyncService();
                            localInstances[typeof(IUserSyncService)] = userSyncService;
                            localInstances[typeof(IOAuthCredentialService)] = new OAuthCredentialService(userSyncService);
                            localInstances[typeof(IDefaultOAuthUserService)] = new DefaultOAuthUserService();
                            localInstances[typeof(IConfigurationManagerService)] = new ConfigurationManagerService();

                            var encode = new Encode();
                            var decode = new Decode();
                            var urlmanipulation = new UrlManipulationService(encode, decode);
                            var urlProxy = new ProxyService(rest);

                            localInstances[typeof (IEncode)] = encode;
                            localInstances[typeof(IDecode)] = decode;
                            localInstances[typeof(IUrlManipulationService)] = urlmanipulation;
                            localInstances[typeof(IProxyService)] = urlProxy;

                            _instances = localInstances;
                        }
            }
           
        }
    }
}
