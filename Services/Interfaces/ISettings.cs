using System;
using System.Collections.Generic;
using System.Text;

namespace Pete.Services.Interfaces
{
    public interface ISettings
    {
        #region Properties
        int Iterations { get; }
        int SaltSize { get; }
        #endregion

        #region Methods
        void Load();
        #endregion
    }
}
