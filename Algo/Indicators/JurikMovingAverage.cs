#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: JurikMovingAverage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Jurik Moving Average.
	/// </summary>
	[DisplayName("JMA")]
	[DescriptionLoc(LocalizedStrings.Str789Key)]
	public class JurikMovingAverage : LengthIndicator<decimal>
	{
		///// <summary>
		///// Текущее направление тренда
		///// </summary>
		//private int _lastDirection;

		int _jj;
		int _ii;

		double _series;

		double _vv;
		double _v1;
		double _v2;
		double _v3;
		double _v4;

		double _s8;
		double _s10;
		double _s18;
		double _s20;

		int _v5;
		int _v6;

		double _s28;
		double _s30;

		int _s38;
		int _s40;
		int _s48;
		int _s50;
		int _s58;
		int _s60;

		double _s68;
		double _s70;

		double _f8;
		double _f10;
		double _f18;
		double _f20;
		double _f28;
		double _f30;
		double _f38;
		double _f40;
		double _f48;
		double _f50;
		double _f58;
		double _f60;
		double _f68;
		double _f70;
		double _f78;
		double _f80;
		double _f88;
		double _f90;
		double _f98;
		double _fA0;
		double _fA8;
		double _fB0;
		double _fB8;
		double _fC0;
		double _fC8;
		double _fD0;
		int _f0;
		int _fD8;
		int _fE0;
		int _fE8;

		int _fF0;

		int _fF8;

		int _value2;

		double[] _list = new double[128];
		double[] _ring1 = new double[128];
		double[] _ring2 = new double[11];
		double[] _buffer = new double[62];

		#region Свойства

		private int _phase;

		/// <summary>
		/// Phase.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str790Key)]
		[DescriptionLoc(LocalizedStrings.Str791Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Phase
		{
			get => _phase;
			set
			{
				_phase = value;

				if (_phase > 100)
					_phase = 100;
				else if (_phase < -100)
					_phase = -100;

				Reset();
			}
		}

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="JurikMovingAverage"/>.
		/// </summary>
		public JurikMovingAverage()
		{
			Initialize();
		}

		/// <summary>
		/// Variables initial initialization.
		/// </summary>
		public void Initialize()
		{
			_jj = 0;
			_ii = 0;

			_series = 0;

			_vv = 0;
			_v1 = 0;
			_v2 = 0;
			_v3 = 0;
			_v4 = 0;

			_s8 = 0;
			_s10 = 0;
			_s18 = 0;
			_s20 = 0;

			_v5 = 0;
			_v6 = 0;

			_s28 = 0;
			_s30 = 0;

			_s38 = 0;
			_s40 = 0;
			_s48 = 0;
			_s50 = 0;
			_s58 = 0;
			_s60 = 0;

			_s68 = 0;
			_s70 = 0;

			_f8 = 0;
			_f10 = 0;
			_f18 = 0;
			_f20 = 0;
			_f28 = 0;
			_f30 = 0;
			_f38 = 0;
			_f40 = 0;
			_f48 = 0;
			_f50 = 0;
			_f58 = 0;
			_f60 = 0;
			_f68 = 0;
			_f70 = 0;
			_f78 = 0;
			_f80 = 0;
			_f88 = 0;
			_f90 = 0;
			_f98 = 0;
			_fA0 = 0;
			_fA8 = 0;
			_fB0 = 0;
			_fB8 = 0;
			_fC0 = 0;
			_fC8 = 0;
			_fD0 = 0;
			_f0 = 0;
			_fD8 = 0;
			_fE0 = 0;
			_fE8 = 0;

			_fF0 = 0;

			_fF8 = 0;

			_value2 = 0;

			_list = new double[128];
			_ring1 = new double[128];
			_ring2 = new double[11];
			_buffer = new double[62];

			_s28 = 63;
			_s30 = 64;

			for (var i = 1; i <= (int)_s28; i++) 
				_list[i] = -1000000;

			for (var i = (int)_s30; i <= 127; i++)
				_list[i] = 1000000;

			_f0 = 1;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var originalLastValue = this.GetCurrentValue();
			var lastValue = originalLastValue;
			var newValue = input.GetValue<decimal>();

			#region Расчет JMA

			_series = (double)newValue;

			if (_fF0 < 61)
			{
				_fF0 = _fF0 + 1;
				_buffer[_fF0] = _series;
			}

			//{ main cycle }
			if (_fF0 > 30)
			{
				if (Length < 1.0000000002)
				{
					_f80 = 0.0000000001; //{1.0e-10}
				}
				else
				{
					_f80 = (Length - 1) / 2.0;
				}

				if (_phase < -100)
				{
					_f10 = 0.5;
				}
				else
				{
					if (_phase > 100)
					{
						_f10 = 2.5;
					}
					else
					{
						_f10 = (double)_phase / 100 + 1.5;
					}
				}

				_v1 = Math.Log(Math.Sqrt(_f80));
				_v2 = _v1;

				if (_v1 / Math.Log(2.0) + 2.0 < 0.0)
				{
					_v3 = 0;
				}
				else
				{
					_v3 = _v2 / Math.Log(2.0) + 2.0;
				}

				_f98 = _v3;

				//----
				if (0.5 <= _f98 - 2.0)
				{
					_f88 = _f98 - 2.0;
				}
				else
				{
					_f88 = 0.5;
				}

				_f78 = Math.Sqrt(_f80) * _f98;
				_f90 = _f78 / (_f78 + 1.0);
				_f80 = _f80 * 0.9;
				_f50 = _f80 / (_f80 + 2.0);

				//----
				if (_f0 != 0)
				{
					_f0 = 0;
					_v5 = 0;

					for (_ii = 1; _ii <= 29; _ii++)
					{
						if (_buffer[_ii + 1] != _buffer[_ii])
						{
							_v5 = 1;
						}
					}

					_fD8 = _v5 * 30;

					_f38 = _fD8 == 0 ? _series : _buffer[1];

					_f18 = _f38;

					if (_fD8 > 29)
						_fD8 = 29;
				}
				else
					_fD8 = 0;

				//----
				for (_ii = _fD8; _ii >= 0; _ii--)
				{
					//{ another bigcycle...}
					_value2 = 31 - _ii;

					_f8 = _ii == 0 ? _series : _buffer[_value2];

					_f28 = _f8 - _f18;
					_f48 = _f8 - _f38;

					_v2 = Math.Max(Math.Abs(_f28), Math.Abs(_f48));

					_fA0 = _v2;
					_vv = _fA0 + 0.0000000001; //{1.0e-10;}

					//----
					if (_s48 <= 1)
					{
						_s48 = 127;
					}
					else
					{
						_s48 = _s48 - 1;
					}

					if (_s50 <= 1)
					{
						_s50 = 10;
					}
					else
					{
						_s50 = _s50 - 1;
					}

					if (_s70 < 128)
						_s70 = _s70 + 1;

					_s8 = _s8 + _vv - _ring2[_s50];
					_ring2[_s50] = _vv;

					if (_s70 > 10)
					{
						_s20 = _s8 / 10;
					}
					else
						_s20 = _s8 / _s70;

					//----
					if (_s70 > 127)
					{
						_s10 = _ring1[_s48];
						_ring1[_s48] = _s20;
						_s68 = 64;
						_s58 = Convert.ToInt32(_s68);

						while (_s68 > 1)
						{
							if (_list[_s58] < _s10)
							{
								_s68 = _s68 * 0.5;
								_s58 = _s58 + Convert.ToInt32(_s68);
							}
							else
								if (_list[_s58] <= _s10)
								{
									_s68 = 1;
								}
								else
								{
									_s68 = _s68 * 0.5;
									_s58 = _s58 - Convert.ToInt32(_s68);
								}
						}
					}
					else
					{
						_ring1[_s48] = _s20;

						if (_s28 + _s30 > 127)
						{
							_s30 = _s30 - 1;
							_s58 = Convert.ToInt32(_s30);
						}
						else
						{
							_s28 = _s28 + 1;
							_s58 = Convert.ToInt32(_s28);
						}

						_s38 = _s28 > 96 ? 96 : Convert.ToInt32(_s28);
						_s40 = _s30 < 32 ? 32 : Convert.ToInt32(_s30);
					}

					//----
					_s68 = 64;
					_s60 = Convert.ToInt32(_s68);

					while (_s68 > 1)
					{
						if (_list[_s60] >= _s20)
						{
							if (_list[_s60 - 1] <= _s20)
							{
								_s68 = 1;
							}
							else
							{
								_s68 = _s68 * 0.5;
								_s60 = _s60 - Convert.ToInt32(_s68);
							}
						}
						else
						{
							_s68 = _s68 * 0.5;
							_s60 = _s60 + Convert.ToInt32(_s68);
						}

						if ((_s60 == 127) && (_s20 > _list[127]))
							_s60 = 128;
					}

					if (_s70 > 127)
					{
						if (_s58 >= _s60)
						{
							if ((_s38 + 1 > _s60) && (_s40 - 1 < _s60))
							{
								_s18 = _s18 + _s20;
							}
							else
								if ((_s40 > _s60) && (_s40 - 1 < _s58))
									_s18 = _s18 + _list[_s40 - 1];
						}
						else
							if (_s40 >= _s60)
							{
								if ((_s38 + 1 < _s60) && (_s38 + 1 > _s58))
									_s18 = _s18 + _list[_s38 + 1];
							}
							else
								if (_s38 + 2 > _s60)
								{
									_s18 = _s18 + _s20;
								}
								else
									if ((_s38 + 1 < _s60) && (_s38 + 1 > _s58))
										_s18 = _s18 + _list[_s38 + 1];

						if (_s58 > _s60)
						{
							if ((_s40 - 1 < _s58) && (_s38 + 1 > _s58))
							{
								_s18 = _s18 - _list[_s58];
							}
							else
								if ((_s38 < _s58) && (_s38 + 1 > _s60))
									_s18 = _s18 - _list[_s38];
						}
						else
						{
							if ((_s38 + 1 > _s58) && (_s40 - 1 < _s58))
							{
								_s18 = _s18 - _list[_s58];
							}
							else
								if ((_s40 > _s58) && (_s40 < _s60))
									_s18 = _s18 - _list[_s40];
						}
					}

					if (_s58 <= _s60)
					{
						if (_s58 >= _s60)
						{
							_list[_s60] = _s20;
						}
						else
						{
							for (_jj = _s58 + 1; _jj <= _s60 - 1; _jj++)
							{
								_list[_jj - 1] = _list[_jj];
							}
							_list[_s60 - 1] = _s20;
						}
					}
					else
					{
						for (_jj = _s58 - 1; _jj >= _s60; _jj--)
						{
							_list[_jj + 1] = _list[_jj];
						}
						_list[_s60] = _s20;
					}

					if (_s70 <= 127)
					{
						_s18 = 0;
						for (_jj = _s40; _jj <= _s38; _jj++)
						{
							_s18 = _s18 + _list[_jj];
						}
					}

					_f60 = _s18 / (_s38 - _s40 + 1);

					if (_fF8 + 1 > 31)
					{
						_fF8 = 31;
					}
					else
						_fF8 = _fF8 + 1;

					//----
					if (_fF8 <= 30)
					{
						if (_f28 > 0)
						{
							_f18 = _f8;
						}
						else
							_f18 = _f8 - _f28 * _f90;

						if (_f48 < 0)
						{
							_f38 = _f8;
						}
						else
							_f38 = _f8 - _f48 * _f90;

						_fB8 = _series;

						if (_fF8 != 30)
						{
							continue;
						}

						if (_fF8 == 30)
						{
							_fC0 = _series;

							_v4 = Math.Max(Math.Ceiling(_f78), 1);

							_fE8 = (int)Math.Ceiling(_v4);

							_v2 = Math.Max(Math.Floor(_f78), 1);

							_fE0 = (int)Math.Ceiling(_v2);

							if (_fE8 == _fE0)
							{
								_f68 = 1;
							}
							else
							{
								_v4 = _fE8 - _fE0;
								_f68 = (_f78 - _fE0) / _v4;
							}

							_v5 = _fE0 <= 29 ? Convert.ToInt32(_fE0) : 29;

							_v6 = _fE8 <= 29 ? Convert.ToInt32(_fE8) : 29;

							_fA8 = (_series - _buffer[_fF0 - _v5]) * (1 - _f68) / _fE0 + (_series - _buffer[_fF0 - _v6]) * _f68 / _fE8;
						}
					}
					else
					{
						_v1 = Math.Min(_f98, Math.Pow(_fA0 / _f60, _f88));

						if (_v1 < 1)
						{
							_v2 = 1;
						}
						else
						{
							_v3 = Math.Min(_f98, Math.Pow(_fA0 / _f60, _f88));

							_v2 = _v3;
						}

						_f58 = _v2;
						_f70 = Math.Pow(_f90, Math.Sqrt(_f58));

						if (_f28 > 0)
						{
							_f18 = _f8;
						}
						else
						{
							_f18 = _f8 - _f28 * _f70;
						}

						if (_f48 < 0)
						{
							_f38 = _f8;
						}
						else
						{
							_f38 = _f8 - _f48 * _f70;
						}
					}
				}

				if (_fF8 > 30)
				{
					_f30 = Math.Pow(_f50, _f58);
					_fC0 = (1 - _f30) * _series + _f30 * _fC0;
					_fC8 = (_series - _fC0) * (1 - _f50) + _f50 * _fC8;
					_fD0 = _f10 * _fC8 + _fC0;
					_f20 = -_f30 * 2;
					_f40 = _f30 * _f30;
					_fB0 = _f20 + _f40 + 1;
					_fA8 = (_fD0 - _fB8) * _fB0 + _f40 * _fA8;
					_fB8 = _fB8 + _fA8;
				}

				lastValue = (decimal)_fB8;
			}

			if (_fF0 <= 30)
			{
				lastValue = newValue;
			}

			#endregion

			//// Добавляем направление тренда
			//_lastDirection = 0;

			//// Сравниваем новое получившееся значение индикатора со старым
			//if (lastValue > originalLastValue)
			//	_lastDirection = 1;
			//else if (lastValue < originalLastValue)
			//	_lastDirection = -1;

			// если буффер стал достаточно большим (стал больше длины)
			if (IsFormed)
			{
				// удаляем хвостовое значение
				Buffer.RemoveAt(0);
			}

			Buffer.Add(newValue);

			return new DecimalIndicatorValue(this, lastValue);
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			Initialize();
			base.Reset();
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Phase = settings.GetValue<int>(nameof(Phase));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue(nameof(Phase), Phase);
		}
	}
}
