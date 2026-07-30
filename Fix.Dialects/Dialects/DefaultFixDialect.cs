namespace StockSharp.Fix.Dialects;

/// <summary>
/// The default implementation of <see cref="IFixDialect"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DefaultFixDialect"/>.
/// </remarks>
/// <param name="transactionIdGenerator">Transaction id generator.</param>
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DefaultKey)]
[MediaIcon(Media.MediaNames.fix)]
public partial class DefaultFixDialect(IdGenerator transactionIdGenerator) : BaseFixDialect(transactionIdGenerator, Encoding.UTF8)
{
	/// <inheritdoc />
	public override IEnumerable<MessageTypeInfo> PossibleSupportedMessages { get; } =
	[
		MessageTypes.MarketData.ToInfo(),
		MessageTypes.SecurityLookup.ToInfo(),
		//MessageTypes.BoardRequest.ToInfo(true),
		MessageTypes.BoardLookup.ToInfo(true),

		MessageTypes.PortfolioLookup.ToInfo(),
		MessageTypes.OrderRegister.ToInfo(),
		MessageTypes.OrderReplace.ToInfo(),
		MessageTypes.OrderCancel.ToInfo(),
		MessageTypes.OrderGroupCancel.ToInfo(),
		MessageTypes.OrderStatus.ToInfo(),

		MessageTypes.ChangePassword.ToInfo(),

		FixMessageTypes.SeqReset.ToInfo(),
		FixMessageTypes.ResendRequest.ToInfo(),

		MessageTypes.EmulationState.ToInfo(),

		MessageTypes.UserInfo.ToInfo(),
		MessageTypes.UserLookup.ToInfo(),
		MessageTypes.UserRequest.ToInfo(),

		MessageTypes.DataTypeLookup.ToInfo(true),

		MessageTypes.SecurityLegsRequest.ToInfo(true),

		MessageTypes.Command.ToInfo(),

		MessageTypes.SubscriptionListRequest.ToInfo(true),
		MessageTypes.SecurityMapping.ToInfo(true),

		MessageTypes.Security.ToInfo(),
		MessageTypes.Remove.ToInfo(),

		MessageTypes.News.ToInfo(),

		MessageTypes.RemoteFileCommand.ToInfo(),
	];

	/// <inheritdoc />
	public override IEnumerable<MessageTypes> NotSupportedResultMessages { get; set; } = [];

	/// <summary>
	/// Convert all non-latin text messages to latin.
	/// </summary>
	public bool ConvertToLatin { get; set; }

	private string Convert(string value) => ConvertToLatin ? value.ToLatin() : value;

	/// <inheritdoc />
	public override bool IsSupportCandlesUpdates(MarketDataMessage subscription) => true;

	/// <inheritdoc />
	public override bool CheckTimeFrameByRequest => true;

	/// <inheritdoc />
	protected override bool IsSupportMarketDataResponse => true;

	/// <inheritdoc />
	public override bool IsAutoReplyOnTransactonalUnsubscription => false;

	/// <inheritdoc />
	public override bool SupportLicensing => true;

	/// <inheritdoc />
	protected override IAsyncEnumerable<Message> OnReadAsync(IFixReader reader, string msgType, CancellationToken cancellationToken)
	{
		return msgType switch
		{
			FixMessages.ExecutionReport => ReadExecutionReportExAsync(reader, cancellationToken),
			FixExtendedMessages.UserInfo => ReadUserInfoAsync(reader, cancellationToken),
			FixExtendedMessages.Board => ReadBoardAsync(reader, cancellationToken),
			FixExtendedMessages.DataTypeInfo
#pragma warning disable CS0612 // Type or member is obsolete
				or FixExtendedMessages.AvailableDataInfo
#pragma warning restore CS0612 // Type or member is obsolete
				=> ReadDataTypeInfo(reader, cancellationToken),
			FixExtendedMessages.SecurityLegsInfo => ReadSecurityLegsInfo(reader, cancellationToken),
			FixExtendedMessages.Subscription => ReadSubscriptionAsync(reader, cancellationToken),
			FixExtendedMessages.SubscriptionResponse => ReadSubscriptionResponseAsync(reader, cancellationToken),
			FixExtendedMessages.SubscriptionFinished => ReadSubscriptionFinishedAsync(reader, cancellationToken),
			FixExtendedMessages.SubscriptionOnline => ReadSubscriptionOnlineAsync(reader, cancellationToken),
			FixExtendedMessages.RemoteFile => ReadRemoteFileAsync(reader, cancellationToken),
			_ => base.OnReadAsync(reader, msgType, cancellationToken),
		};
	}

	/// <inheritdoc />
	protected override async ValueTask<string> OnWriteAsync(IFixWriter writer, Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.OrderRegister:
			{
				return await WriteNewOrderSingleAsync(writer, (OrderRegisterMessage)message, cancellationToken);
			}

			case MessageTypes.OrderCancel:
			{
				return await WriteOrderCancelRequestAsync(writer, (OrderCancelMessage)message, cancellationToken);
			}

			case MessageTypes.OrderReplace:
			{
				return await WriteOrderCancelReplaceRequestAsync(writer, (OrderReplaceMessage)message, cancellationToken);
			}

			case MessageTypes.OrderGroupCancel:
			{
				return await WriteOrderMassCancelRequestAsync(writer, (OrderGroupCancelMessage)message, cancellationToken);
			}

			case MessageTypes.OrderStatus:
			{
				var statusMsg = (OrderStatusMessage)message;

				if (statusMsg.IsSubscribe)
				{
					if (statusMsg.OrderId != null || statusMsg.OriginalTransactionId != 0)
					{
						return await WriteOrderStatusRequestAsync(writer, statusMsg, cancellationToken);
					}
					else
					{
						return await WriteOrderMassStatusRequestAsync(writer, statusMsg, cancellationToken);
					}
				}
				else
					return await WriteOrderMassStatusRequestAsync(writer, statusMsg, cancellationToken);
			}

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				if (mdMsg.TransactionId == default)
					throw new InvalidOperationException("TransId==0");

				var requestId = mdMsg.TransactionId.To<string>();
				var responseId = mdMsg.OriginalTransactionId == 0 ? null : mdMsg.OriginalTransactionId.To<string>();

				this.AddInfoLog(mdMsg.ToString());

				await writer.WriteMarketDataMessageAsync(mdMsg, requestId, responseId, TimeStampParser, WriteSecurityIdAsync, cancellationToken);

				return FixMessages.MarketDataRequest;
			}

			case MessageTypes.SecurityLookup:
			{
				return await WriteSecurityListRequestAsync(writer, (SecurityLookupMessage)message, cancellationToken);
			}

			case MessageTypes.BoardLookup:
			{
				return await WriteBoardLookupAsync(writer, (BoardLookupMessage)message, cancellationToken);
			}

			//case MessageTypes.BoardRequest:
			//{
			//	return WriteTradingSessionStatusRequest(writer, (BoardRequestMessage)message);
			//}

			case MessageTypes.PortfolioLookup:
			{
				return await WriteRequestForPositionsAsync(writer, (PortfolioLookupMessage)message, cancellationToken);
			}

			case MessageTypes.EmulationState:
			{
				return await WriteEmulationStateAsync(writer, (EmulationStateMessage)message, cancellationToken);
			}

			case MessageTypes.UserInfo:
			{
				return await WriteUserInfoAsync(writer, (UserInfoMessage)message, cancellationToken);
			}

			case MessageTypes.UserLookup:
			{
				return await WriteUserLookupAsync(writer, (UserLookupMessage)message, cancellationToken);
			}

			case MessageTypes.UserRequest:
			{
				return await WriteUserRequestAsync(writer, (UserRequestMessage)message, cancellationToken);
			}

			case MessageTypes.Security:
			{
				return await WriteSecurityUploadAsync(writer, (SecurityMessage)message, cancellationToken);
			}

			case MessageTypes.DataTypeLookup:
			{
				return await WriteDataTypeLookupAsync(writer, (DataTypeLookupMessage)message, cancellationToken);
			}

			case MessageTypes.SecurityLegsRequest:
			{
				return await WriteSecurityLegsRequestAsync(writer, (SecurityLegsRequestMessage)message, cancellationToken);
			}

			case MessageTypes.Command:
			{
				await writer.WriteCommandAsync((CommandMessage)message, TimeStampParser, (w, m, ct) => default, cancellationToken);
				return FixExtendedMessages.Command;
			}

			case MessageTypes.SubscriptionListRequest:
			{
				return await WriteSubscriptionListRequestAsync(writer, (SubscriptionListRequestMessage)message, cancellationToken);
			}

			case MessageTypes.SecurityMapping:
			{
				return await WriteSecurityMappingAsync(writer, (SecurityMappingMessage)message, cancellationToken);
			}

			case MessageTypes.Remove:
			{
				return await WriteRemoveAsync(writer, (RemoveMessage)message, cancellationToken);
			}

			case MessageTypes.News:
			{
				return await WriteNewsAsync(writer, (NewsMessage)message, cancellationToken);
			}

			case MessageTypes.RemoteFileCommand:
			{
				return await WriteRemoteFileCommandAsync(writer, (RemoteFileCommandMessage)message, cancellationToken);
			}

			case MessageTypes.RemoteFile:
			{
				var rfm = (RemoteFileMessage)message;

				if (rfm.TransactionId != default)
				{
					await writer.WriteAsync(FixTags.MDReqID, cancellationToken);
					await writer.WriteAsync(rfm.TransactionId.To<string>(), cancellationToken);
				}

				await writer.WriteFileAsync(rfm, cancellationToken);

				return FixExtendedMessages.RemoteFile;
			}

			case FixMessageTypes.UserRequest:
			{
				return await WriteUserRequestMessageAsync(writer, (FixUserRequestMessage)message, cancellationToken);
			}

			default:
				return await base.OnWriteAsync(writer, message, cancellationToken);
		}
	}

	private async ValueTask<string> WriteUserRequestMessageAsync(IFixWriter writer, FixUserRequestMessage message, CancellationToken cancellationToken)
	{
		if (message.TransactionId != default)
		{
			await writer.WriteAsync(FixTags.UserRequestID, cancellationToken);
			await writer.WriteAsync(message.TransactionId.To<string>(), cancellationToken);
		}

		await writer.WriteAsync(FixTags.UserRequestType, cancellationToken);
		await writer.WriteAsync((int)message.RequestType, cancellationToken);

		if (!message.OldPassword.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Password, cancellationToken);
			await writer.WriteAsync(message.OldPassword.UnSecure(), cancellationToken);
		}

		if (!message.NewPassword.IsEmpty())
		{
			await writer.WriteAsync(FixTags.NewPassword, cancellationToken);
			await writer.WriteAsync(message.NewPassword.UnSecure(), cancellationToken);
		}

		if (!message.UserName.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Username, cancellationToken);
			await writer.WriteAsync(message.UserName, cancellationToken);
		}

		return FixMessages.UserRequest;
	}

	private async IAsyncEnumerable<Message> ReadUserInfoAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string requestId = null;

		var result = reader.ReadUserInfoMessageAsync(TimeStampParser, async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.UserRequestID:
					requestId = await reader.ReadStringAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		await foreach (Message msg in result.WithEnforcedCancellation(cancellationToken))
		{
			((IOriginalTransactionIdMessage)msg).OriginalTransactionId = requestId.To<long>();
			yield return msg;
		}
	}

	private async ValueTask<string> WriteUserInfoAsync(IFixWriter writer, UserInfoMessage message, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.UserRequestID, cancellationToken);
		await writer.WriteAsync(message.TransactionId.To<string>(), cancellationToken);

		await writer.WriteUserInfoMessageAsync(message, TimeStampParser, cancellationToken);

		return FixExtendedMessages.UserInfo;
	}

	private async ValueTask<string> WriteUserLookupAsync(IFixWriter writer, UserLookupMessage message, CancellationToken cancellationToken)
	{
		await writer.WriteSubscriptionRequestAsync(message, TimeStampParser, FixTags.UserRequestID, cancellationToken);

		if (message.IsSubscribe)
		{
			if (!message.Like.IsEmpty())
			{
				await writer.WriteAsync(FixTags.Username, cancellationToken);
				await writer.WriteAsync(message.Like, cancellationToken);
			}

			if (message.UserId != null)
			{
				await writer.WriteAsync(FixTags.Id, cancellationToken);
				await writer.WriteAsync(message.UserId.Value, cancellationToken);
			}

			if (message.Own)
			{
				await writer.WriteAsync(FixTags.Owner, cancellationToken);
				await writer.WriteAsync(message.Own, cancellationToken);
			}
		}

		await writer.WriteAsync(FixTags.UserRequestType, cancellationToken);
		await writer.WriteAsync(0, cancellationToken);

		return FixExtendedMessages.UserRequest;
	}

	private static async ValueTask<string> WriteUserRequestAsync(IFixWriter writer, UserRequestMessage message, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.UserRequestID, cancellationToken);
		await writer.WriteAsync(message.TransactionId.To<string>(), cancellationToken);

		if (!message.Login.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Username, cancellationToken);
			await writer.WriteAsync(message.Login, cancellationToken);
		}

		if (message.Id != null)
		{
			await writer.WriteAsync(FixTags.Id, cancellationToken);
			await writer.WriteAsync(message.Id.Value, cancellationToken);
		}

		await writer.WriteAsync(FixTags.SubscriptionRequestType, cancellationToken);
		await writer.WriteAsync(message.GetSubscriptionType(), cancellationToken);

		return FixMessages.UserRequest;
	}

	private async ValueTask<string> WriteEmulationStateAsync(IFixWriter writer, EmulationStateMessage message, CancellationToken cancellationToken)
	{
		if (message.State == ChannelStates.Starting)
		{
			await writer.WriteAsync(FixTags.StartDate, cancellationToken);
			await writer.WriteUtcAsync(message.StartDate, TimeStampParser, cancellationToken);

			await writer.WriteAsync(FixTags.EndDate, cancellationToken);
			await writer.WriteUtcAsync(message.StopDate, TimeStampParser, cancellationToken);
		}

		return message.State == ChannelStates.Starting ? FixExtendedMessages.HistoryStart : FixExtendedMessages.HistoryEnd;
	}

	private static async ValueTask WriteSecurityIdAsync(IFixWriter writer, SecurityMessage secMsg, CancellationToken cancellationToken)
	{
		if (secMsg.SecurityId != default)
		{
			await writer.WriteAsync(FixTags.Symbol, cancellationToken);
			await writer.WriteAsync(secMsg.SecurityId.SecurityCode, cancellationToken);

			await writer.WriteAsync(FixTags.SecurityExchange, cancellationToken);
			await writer.WriteAsync(secMsg.SecurityId.BoardCode, cancellationToken);
		}

		if (!secMsg.CfiCode.IsEmpty())
		{
			await writer.WriteAsync(FixTags.CFICode, cancellationToken);
			await writer.WriteAsync(secMsg.CfiCode, cancellationToken);
		}

		if (secMsg.SecurityType != null)
		{
			await writer.WriteAsync(FixTags.SecurityType, cancellationToken);
			await writer.WriteAsync(secMsg.SecurityType.Value.ToFix(), cancellationToken);
		}
	}
}
