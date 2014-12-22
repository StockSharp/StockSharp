function varargout = MainWindow(varargin)
% MainWindow MATLAB code for MainWindow.fig
%      MainWindow, by itself, creates a new MainWindow or raises the existing
%      singleton*.
%
%      H = MainWindow returns the handle to a new MainWindow or the handle to
%      the existing singleton*.
%
%      MainWindow('CALLBACK',hObject,eventData,handles,...) calls the local
%      function named CALLBACK in MainWindow.M with the given input arguments.
%
%      MainWindow('Property','Value',...) creates a new MainWindow or raises the
%      existing singleton*.  Starting from the left, property value pairs are
%      applied to the GUI before MainWindow_OpeningFcn gets called.  An
%      unrecognized property name or invalid value makes property application
%      stop.  All inputs are passed to MainWindow_OpeningFcn via varargin.
%
%      *See GUI Options on GUIDE's Tools menu.  Choose "GUI allows only one
%      instance to run (singleton)".
%
% See also: GUIDE, GUIDATA, GUIHANDLES

% Edit the above text to modify the response to help MainWindow

% Last Modified by GUIDE v2.5 14-Mar-2012 23:01:38

% Begin initialization code - DO NOT EDIT
gui_Singleton = 1;
gui_State = struct('gui_Name',       mfilename, ...
                   'gui_Singleton',  gui_Singleton, ...
                   'gui_OpeningFcn', @MainWindow_OpeningFcn, ...
                   'gui_OutputFcn',  @MainWindow_OutputFcn, ...
                   'gui_LayoutFcn',  [] , ...
                   'gui_Callback',   []);
if nargin && ischar(varargin{1})
    gui_State.gui_Callback = str2func(varargin{1});
end

if nargout
    [varargout{1:nargout}] = gui_mainfcn(gui_State, varargin{:});
else
    gui_mainfcn(gui_State, varargin{:});
end
% End initialization code - DO NOT EDIT

% --- Executes just before MainWindow is made visible.
function MainWindow_OpeningFcn(hObject, eventdata, handles, varargin)
% This function has no output args, see OutputFcn.
% hObject    handle to figure
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
% varargin   command line arguments to MainWindow (see VARARGIN)

% Choose default command line output for MainWindow
handles.output = hObject;

% Update handles structure
guidata(hObject, handles);

NET.addAssembly('System.Core');
NET.addAssembly(char(System.IO.Path.GetFullPath(strcat(pwd, '..\..\..\References\StockSharp.Quik.dll'))));
NET.addAssembly(char(System.IO.Path.GetFullPath(strcat(pwd, '..\..\..\References\StockSharp.MatLab.dll'))));

set(handles.secListBox, 'string', cell(0, 1));

data = get(handles.tradesTable, 'Data');
set(handles.tradesTable, 'Data', cell(0, size(data, 2)));

data = get(handles.ordersTable, 'Data');
set(handles.ordersTable, 'Data', cell(0, size(data, 2)));

data = get(handles.positionsTable, 'Data');
set(handles.positionsTable, 'Data', cell(0, size(data, 2)));

% UIWAIT makes MainWindow wait for user response (see UIRESUME)
% uiwait(MainWindow.MainWindow);


% --- Outputs from this function are returned to the command line.
function varargout = MainWindow_OutputFcn(hObject, eventdata, handles) 
% varargout  cell array for returning output args (see VARARGOUT);
% hObject    handle to figure
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Get default command line output from handles structure
varargout{1} = handles.output;


% --- Executes on button press in connectBtn.
function connectBtn_Callback(hObject, eventdata, handles)
% hObject    handle to connectBtn (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

global hGui;
hGui = handles;

global connector;

try
    if (isempty(connector) == 0)
        Dispose(connector);
        delete(connector);
        clearvars -global connector;
    end

    global connector;
    % создание подключение для Квика и передача его в MatLabConnector
    connector = StockSharp.MatLab.MatLabConnector(StockSharp.Quik.Quik//connector.);

    % подписка на события получения маркет-данных, информации по заявкам и
    % сделкам
    addlistener(connector, 'Connected', @connectorConnected);
    addlistener(connector, 'NewSecurities', @newSecurities);
    addlistener(connector, 'SecuritiesChanged', @securitiesChanged);
    addlistener(connector, 'NewPortfolios', @newPortfolios);
    addlistener(connector, 'NewOrders', @newOrders);
    addlistener(connector, 'OrdersChanged', @ordersChanged);
    addlistener(connector, 'NewMyTrades', @newMyTrades);
    addlistener(connector, 'NewPositions', @newPositions);
    addlistener(connector, 'PositionsChanged', @positionsChanged);
    addlistener(connector, 'ProcessDataError', @processError);
    addlistener(connector, 'ConnectionError', @processError);
    
    set(handles.secListBox, 'string', cell(0, 1));

    data = get(handles.tradesTable, 'Data');
    set(handles.tradesTable, 'Data', cell(0, size(data, 2)));

    data = get(handles.ordersTable, 'Data');
    set(handles.ordersTable, 'Data', cell(0, size(data, 2)));

    data = get(handles.positionsTable, 'Data');
    set(handles.positionsTable, 'Data', cell(0, size(data, 2)));
    
    Connect(connector.RealConnector);
catch error
    msgbox(char(error.message))
end


function connectorConnected(sender, args)
global connector;
StartExport(connector.RealConnector);

function processError(sender, args)
disp(char(args.Error.message))

function newSecurities(sender, args)
global hGui;

set(hGui.selectSecurityBtn, 'Enable', 'on')

data = cellstr(get(hGui.secListBox, 'String'));

for i = 1 : args.Securities.Length
    data = [data; char(args.Securities(i).Id)];
end
    
set(hGui.secListBox, 'string', data);

function securitiesChanged(sender, args)
global selectedSecurity;
global hGui;

% обновление ластов по инструментам

for i = 1 : args.Securities.Length
    security = args.Securities(i);
    
	if (security == selectedSecurity)
        if (isempty(security.LastTrade) == 0)
            set(hGui.lastTradeTxt, 'String', char(ToString(security.LastTrade)))
        end
        
        if (isempty(security.BestBid) == 0)
            set(hGui.bestBidTxt, 'String', char(ToString(security.BestBid)))
        end
        
        if (isempty(security.BestAsk) == 0)
            set(hGui.bestAskTxt, 'String', char(ToString(security.BestAsk)))
        end
    end
end

function newPortfolios(sender, args)
global hGui;

data = cellstr(get(hGui.portfoliosCombo, 'string'));
    
for i = 1 : args.Portfolios.Length
	data = [data; char(args.Portfolios(i).Name)];
end
    
set(hGui.portfoliosCombo, 'string', data);

function newOrders(sender, args)
global hGui;

data = get(hGui.ordersTable, 'Data');

newRows = cell(args.Orders.Length, size(data, 2));

for i = 1 : args.Orders.Length
    order = args.Orders(i);

    newRows{i, 1} = order.TransactionId;
    newRows{i, 2} = char(ToString(order.Direction));
    newRows{i, 3} = char(ToString(order.Time));
    newRows{i, 4} = char(ToString(order.Price));
    newRows{i, 5} = char(ToString(order.Volume));
    newRows{i, 6} = char(ToString(order.Balance));
    newRows{i, 7} = order.Id;
    newRows{i, 8} = char(ToString(order.State));
end

data = cat(1, data, newRows)

set(hGui.ordersTable, 'Data', data)

function ordersChanged(sender, args)
global hGui;

data = get(hGui.ordersTable, 'Data');

for i = 1 : args.Orders.Length
    order = args.Orders(i);
    
    for j = 1 : size(data, 1)
        if (data{j, 1} == order.TransactionId)
            
            data{j, 3} = char(ToString(order.Time));
            data{j, 6} = char(ToString(order.Balance));
            data{j, 7} = order.Id;
            data{j, 8} = char(ToString(order.State));
    
            break;
        end
    end
end

set(hGui.ordersTable, 'Data', data)

function newPositions(sender, args)
global hGui;

data = get(hGui.positionsTable, 'Data');

newRows = cell(args.Positions.Length, size(data, 2));

for i = 1 : args.Positions.Length
    position = args.Positions(i);

    newRows{i, 1} = char(position.Security.Id);
    newRows{i, 2} = char(ToString(position.CurrentValue));
end

data = cat(1, data, newRows);

set(hGui.positionsTable, 'Data', data)

function positionsChanged(sender, args)
global hGui;

data = get(hGui.positionsTable, 'Data');

for i = 1 : args.Positions.Length
    position = args.Positions(i);
    
    for j = 1 : size(data, 1)
        if (strcmp(data{j, 1}, char(position.Security.Id)) == 1)
            data{j, 2} = char(ToString(position.CurrentValue));
            break;
        end
    end
end

set(hGui.positionsTable, 'Data', data)

function newMyTrades(sender, args)
global hGui;

data = get(hGui.tradesTable, 'Data');

newRows = cell(args.Trades.Length, size(data, 2));

for i = 1 : args.Trades.Length
    trade = args.Trades(i);

    newRows{i, 1} = trade.Trade.Id;
    newRows{i, 2} = char(ToString(trade.Trade.Time));
    newRows{i, 3} = char(ToString(trade.Trade.Price));
    newRows{i, 4} = char(ToString(trade.Trade.Volume));
    newRows{i, 5} = trade.Order.Id;
end

data = cat(1, data, newRows);

set(hGui.tradesTable, 'Data', data)

% --- Executes on selection change in secListBox.
function secListBox_Callback(hObject, eventdata, handles)
% hObject    handle to secListBox (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: contents = cellstr(get(hObject,'String')) returns secListBox contents as cell array
%        contents{get(hObject,'Value')} returns selected item from secListBox


% --- Executes during object creation, after setting all properties.
function secListBox_CreateFcn(hObject, eventdata, handles)
% hObject    handle to secListBox (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: listbox controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end


% --- Executes on button press in newOrderBtn.
function newOrderBtn_Callback(hObject, eventdata, handles)
% hObject    handle to newOrderBtn (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
global connector;
global selectedSecurity;

order = StockSharp.BusinessEntities.Order();
order.Security = selectedSecurity;
order.Price = str2double(get(handles.priceTxt, 'String'));
order.Volume = str2double(get(handles.volumeTxt, 'String'));

if (get(handles.isBuyChBox, 'Value') == 1)
    order.Direction = StockSharp.BusinessEntities.OrderDirections.Buy;
else
    order.Direction = StockSharp.BusinessEntities.OrderDirections.Sell;
end

portfolios = NET.invokeGenericMethod('System.Linq.Enumerable', 'ToArray', ...
    {'StockSharp.BusinessEntities.Portfolio'}, connector.RealConnector.Portfolios);

portfolioNames = get(handles.portfoliosCombo, 'String');
portfolioName = portfolioNames{get(handles.portfoliosCombo, 'Value')};

for i = 1 : portfolios.Length
    portfolio = portfolios(i);
    
    if (strcmp(char(portfolio.Name), portfolioName) == 1)
        order.Portfolio = portfolio;
        break;
    end
end

RegisterOrder(connector.RealConnector, order);

% --- Executes on button press in selectSecurityBtn.
function selectSecurityBtn_Callback(hObject, eventdata, handles)
% hObject    handle to selectSecurityBtn (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
securityIds = get(handles.secListBox, 'String');
selectedSecurityId = securityIds{get(handles.secListBox, 'Value')};

global connector;
global selectedSecurity;

securities = NET.invokeGenericMethod('System.Linq.Enumerable', 'ToArray', ...
    {'StockSharp.BusinessEntities.Security'}, connector.RealConnector.Securities);

for i = 1 : securities.Length
    security = securities(i);
    
    if (strcmp(char(security.Id), selectedSecurityId) == 1)
        selectedSecurity = security;
        RegisterSecurity(connector.RealConnector, security);
        RegisterTrades(connector.RealConnector, security);
        RegisterMarketDepth(connector.RealConnector, security);
        set(handles.newOrderBtn, 'Enable', 'on')
        break;
    end
end


% --- Executes during object creation, after setting all properties.
function MainWindow_CreateFcn(hObject, eventdata, handles)
% hObject    handle to MainWindow (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called



function priceTxt_Callback(hObject, eventdata, handles)
% hObject    handle to priceTxt (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: get(hObject,'String') returns contents of priceTxt as text
%        str2double(get(hObject,'String')) returns contents of priceTxt as a double


% --- Executes during object creation, after setting all properties.
function priceTxt_CreateFcn(hObject, eventdata, handles)
% hObject    handle to priceTxt (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: edit controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end



function volumeTxt_Callback(hObject, eventdata, handles)
% hObject    handle to volumeTxt (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: get(hObject,'String') returns contents of volumeTxt as text
%        str2double(get(hObject,'String')) returns contents of volumeTxt as a double


% --- Executes during object creation, after setting all properties.
function volumeTxt_CreateFcn(hObject, eventdata, handles)
% hObject    handle to volumeTxt (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: edit controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end


% --- Executes on button press in isBuyChBox.
function isBuyChBox_Callback(hObject, eventdata, handles)
% hObject    handle to isBuyChBox (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hint: get(hObject,'Value') returns toggle state of isBuyChBox


% --- Executes on selection change in portfoliosCombo.
function portfoliosCombo_Callback(hObject, eventdata, handles)
% hObject    handle to portfoliosCombo (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: contents = cellstr(get(hObject,'String')) returns portfoliosCombo contents as cell array
%        contents{get(hObject,'Value')} returns selected item from portfoliosCombo


% --- Executes during object creation, after setting all properties.
function portfoliosCombo_CreateFcn(hObject, eventdata, handles)
% hObject    handle to portfoliosCombo (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: popupmenu controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end


% --- Executes during object deletion, before destroying properties.
function MainWindow_DeleteFcn(hObject, eventdata, handles)
% hObject    handle to MainWindow (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
global connector;

if (isempty(connector) == 0)
    Dispose(connector);
    delete(connector);
    clearvars -global connector;
end


% --- Executes during object deletion, before destroying properties.
function tradesTable_DeleteFcn(hObject, eventdata, handles)
% hObject    handle to tradesTable (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
