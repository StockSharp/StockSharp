var _scrollablePanelIds = new Array();
var _scrollablePanelIdsCount = 0;

function PutScrollablePanelNewId(id)
{
	_scrollablePanelIds[_scrollablePanelIdsCount++] = id;
}

//Start fleXcroll using any method you like, either inside your html like this, or you may use seperate files
//The latter is more ideal, but this code is here for easy viewing
if (document.getElementById && document.getElementsByTagName)
{
	var func = function()
	{
		for (var i = 0; i < _scrollablePanelIdsCount; i++)
		{
			CSBfleXcroll(_scrollablePanelIds[i]);
		}
	};

	if (window.addEventListener)
		window.addEventListener('load', func, false);
	else if (window.attachEvent)
		window.attachEvent('onload', func);
}