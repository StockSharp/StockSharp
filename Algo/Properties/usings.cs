global using System;
global using System.Text;
global using System.Collections;
global using System.Collections.Generic;
global using System.Runtime.Serialization;
global using System.Runtime.CompilerServices;
global using System.Linq;
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Reflection;
global using System.Threading;
global using System.Threading.Tasks;
global using System.IO;

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

global using Nito.AsyncEx;

global using StockSharp.Configuration;
global using StockSharp.Localization;
global using StockSharp.Messages;
global using StockSharp.BusinessEntities;
global using StockSharp.Algo.Storages;
global using StockSharp.Algo.Compilation;
global using DataType = StockSharp.Messages.DataType;
global using Extensions = StockSharp.Messages.Extensions;