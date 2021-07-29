using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Pete.Services.Interfaces;
using TNO.Json.Parsing;
using TNO.Json.Runtime;

namespace Pete.Services
{
    public class Settings : ISettings
    {
        #region Consts
        private const int DEF_ITER = 2_500_000;
        private const int DEF_SALT = 10240;
        private const string PATH_DATA = "data";
        private const string PATH_SETTINGS = PATH_DATA + "\\settings.json";
        #endregion

        #region Properties
        public int Iterations { get; private set; } = DEF_ITER;
        public int SaltSize { get; private set; } = DEF_SALT;
        #endregion

        #region Methods
        public void Load()
        {
            if (File.Exists(PATH_SETTINGS))
            {
                try
                {
                    JsonValue json = Parser.QuickParse(File.ReadAllText(PATH_SETTINGS));
                    Iterations = json["iterations"];
                    SaltSize = json["salt_size"];

                    bool updt = false;
                    if (Iterations < DEF_ITER)
                    {
                        Iterations = DEF_ITER;
                        updt = true;
                    }
                    if (SaltSize < DEF_SALT)
                    {
                        SaltSize = DEF_SALT;
                        updt = true;
                    }

                    if (updt) SaveDefault(false);
                }
                catch { }
            }
            SaveDefault(true);
        }
        private void SaveDefault(bool restore)
        {
            if (restore)
            {
                Iterations = DEF_ITER;
                SaltSize = DEF_SALT;
            }
            JsonObject obj = new JsonObject()
            {
                {"iterations", Iterations},
                {"salt_size",SaltSize },
            };
            string json = obj.Format();
            if (!Directory.Exists(PATH_DATA)) Directory.CreateDirectory(PATH_DATA);

            File.WriteAllText(PATH_SETTINGS, json);
        }
        #endregion
    }
}
