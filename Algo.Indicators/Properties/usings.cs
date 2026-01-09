global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Reflection;

global using Ecng.Common;
global using Ecng.Collections;
global using Ecng.Serialization;
global using Ecng.ComponentModel;
global using Ecng.Logging;
global using Ecng.Linq;
global using Ecng.IO;
#if NET7_0_OR_GREATER
global using DecimalBuffer = Ecng.Collections.NumericCircularBufferEx<decimal>;
#else
global using DecimalBuffer = Ecng.Collections.CircularBufferEx<decimal>;
#endif

global using StockSharp.Localization;
global using StockSharp.Messages;
global using StockSharp.BusinessEntities;