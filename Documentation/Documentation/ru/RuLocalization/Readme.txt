Для SHFB 2016
1. Заменить C:\Program Files (x86)\EWSoftware\Sandcastle Help File Builder\PresentationStyles\VS2013\icons\favicon.ico на S# иконку.

Для SHFB 1.9.5
1. Заменить C:\Program Files (x86)\EWSoftware\Sandcastle Help File Builder\Web\favicon.ico на S# иконку.

Для SHFB 1.9.4

1. Файл syntax_content.xml скопировать в "C:\Program Files (x86)\Sandcastle\Presentation\Shared\content\ru-RU" 
2. Папку ru-RU скопировать в "C:\Program Files (x86)\Sandcastle\Presentation\vs2010\Content\" (vs2010 - это название стиля, так что если используется другое, то путь нужно поменять).
3. Файл VS2010BuilderContent_ru-RU.xml скопировать в "C:\Program Files (x86)\EWSoftware\Sandcastle Help File Builder\SharedContent\"
4. Для фикс http://shfb.codeplex.com/workitem/32733 и замены favicon.ico на favicon.png, перезаписать branding в "C:\Program Files (x86)\Sandcastle\Presentation\vs2010\Branding\"