<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
								xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
								xmlns:msxsl="urn:schemas-microsoft-com:xslt"
								exclude-result-prefixes="msxsl branding"
								xmlns:mtps="http://msdn2.microsoft.com/mtps"
								xmlns:xhtml="http://www.w3.org/1999/xhtml"
								xmlns:branding="urn:FH-Branding"
								xmlns:xs="http://www.w3.org/2001/XMLSchema"
								xmlns:cs="urn:Get-Paths"

>
	<xsl:import href="head.xslt"/>

	<xsl:template match="xhtml:head"
								name="ps-head">
		<xsl:copy>
			<xsl:call-template name="head-favicon"/>
			<xsl:if test="not($pre-branding)">
				<xsl:call-template name="head-stylesheet"/>
			</xsl:if>
			<xsl:call-template name="head-style-urls"/>
			<xsl:if test="$downscale-browser">
				<xsl:call-template name="head-styles-external"/>
			</xsl:if>
			<xsl:if test="not($pre-branding)">
				<xsl:call-template name="head-script"/>
			</xsl:if>

			<xsl:apply-templates select="@*"/>
			<xsl:apply-templates select="node()"/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="xhtml:head//xhtml:meta"
								name="ps-head-meta">
		<xsl:copy-of select="."/>
	</xsl:template>

	<xsl:template match="xhtml:head//xhtml:xml"
								name="ps-head-xml">
		<xsl:copy-of select="."/>
	</xsl:template>

	<!-- Remove branding data from the header - it's no longer required -->
	<xsl:template match="/xhtml:html/xhtml:head/xhtml:xml[@id='BrandingData']"/>

	<!-- ============================================================================================
	Header Parts
	============================================================================================= -->

	<xsl:template name="head-favicon">
		<xsl:element name="link"
								 namespace="{$xhtml}">
			<xsl:attribute name="rel">
				<xsl:value-of select="'SHORTCUT ICON'"/>
			</xsl:attribute>
			<xsl:attribute name="href">
				<xsl:call-template name="ms-xhelp">
					<xsl:with-param name="ref"
													select="'favicon.png'"/>
				</xsl:call-template>
			</xsl:attribute>
		</xsl:element>
	</xsl:template>

	<xsl:template name="head-stylesheet">
		<xsl:element name="link"
								 namespace="{$xhtml}">
			<xsl:attribute name="rel">
				<xsl:value-of select="'stylesheet'"/>
			</xsl:attribute>
			<xsl:attribute name="type">
				<xsl:value-of select="'text/css'"/>
			</xsl:attribute>
			<xsl:attribute name="href">
				<xsl:call-template name="ms-xhelp">
					<xsl:with-param name="ref"
													select="$css-file"/>
				</xsl:call-template>
			</xsl:attribute>
		</xsl:element>
	</xsl:template>

	<xsl:template name="head-style-urls">
		<xsl:element name="style"
								 namespace="{$xhtml}">
			<xsl:attribute name="type">text/css</xsl:attribute>
			<xsl:text>.OH_CodeSnippetContainerTabLeftActive, .OH_CodeSnippetContainerTabLeft,.OH_CodeSnippetContainerTabLeftDisabled {background-image: url('</xsl:text>
			<xsl:call-template name="ms-xhelp">
				<xsl:with-param name="ref"
												select="'tabLeftBG.gif'"/>
			</xsl:call-template>
			<xsl:text>')}</xsl:text>
			<xsl:text>.OH_CodeSnippetContainerTabRightActive, .OH_CodeSnippetContainerTabRight,.OH_CodeSnippetContainerTabRightDisabled {background-image: url('</xsl:text>
			<xsl:call-template name="ms-xhelp">
				<xsl:with-param name="ref"
												select="'tabRightBG.gif'"/>
			</xsl:call-template>
			<xsl:text>')}</xsl:text>
			<xsl:text>.OH_footer { background-image: url('</xsl:text>
			<xsl:call-template name="ms-xhelp">
				<xsl:with-param name="ref"
												select="'footer_slice.gif'"/>
			</xsl:call-template>
			<xsl:text>'); background-position:top; background-repeat:repeat-x}</xsl:text>
		</xsl:element>
	</xsl:template>

	<xsl:template name="head-style-urls-fixup">
		<xsl:if test="$pre-branding">
			<xsl:element name="script"
									 namespace="{$xhtml}">
				<xsl:attribute name="type">
					<xsl:value-of select="'text/javascript'"/>
				</xsl:attribute>
				<xsl:variable name="v_script">
					<![CDATA[
					var iconPath = undefined;
					try
					{
						var linkEnum = new Enumerator(document.getElementsByTagName('link'));
						var link;

						for (linkEnum.moveFirst(); !linkEnum.atEnd(); linkEnum.moveNext())
						{
							link = linkEnum.item();
							if (link.rel.toLowerCase() == 'shortcut icon')
							{
								iconPath = link.href.toString();
								iconPath = iconPath.substring(0,iconPath.lastIndexOf(";")) + ";";
								break;
							}
						}
					}
					catch (e) {}
					finally {}
					if (iconPath)
					{
						try
						{
							var styleSheetEnum = new Enumerator(document.styleSheets);
							var styleSheet;
							var ruleNdx;
							var rule;

							for (styleSheetEnum.moveFirst(); !styleSheetEnum.atEnd(); styleSheetEnum.moveNext())
							{
								styleSheet = styleSheetEnum.item();
								if (styleSheet.rules)
								{
									if (styleSheet.rules.length != 0)
									{
										for (ruleNdx = 0; ruleNdx != styleSheet.rules.length; ruleNdx++)
										{
											rule = styleSheet.rules.item(ruleNdx);

											var bgUrl = rule.style.backgroundImage;
											if (bgUrl != "")
											{
												bgUrl = bgUrl.substring(bgUrl.indexOf("(")+1,bgUrl.lastIndexOf(")"));
											}
											if (bgUrl != "")
											{
												rule.style.backgroundImage = "url(" + iconPath + bgUrl + ")";
											}
										}
									}
								}
							}
						}
						catch (e) {}
						finally {}
					}
					]]>
				</xsl:variable>
				<xsl:value-of select="$v_script"
											disable-output-escaping="yes"/>
			</xsl:element>
		</xsl:if>
	</xsl:template>

	<xsl:template name="head-styles-external">
		<xsl:element name="style"
								 namespace="{$xhtml}">
			<xsl:attribute name="type">text/css</xsl:attribute>
			body
			{
			border-left:5px solid #e6e6e6;
			overflow-x:scroll;
			overflow-y:scroll;
			}
		</xsl:element>
	</xsl:template>

	<xsl:template name="head-script">
		<xsl:element name="script"
								 namespace="{$xhtml}">
			<xsl:attribute name="src">
				<xsl:call-template name="ms-xhelp">
					<xsl:with-param name="ref"
													select="$js-file"/>
				</xsl:call-template>
			</xsl:attribute>
			<xsl:attribute name="type">
				<xsl:value-of select="'text/javascript'"/>
			</xsl:attribute>
			<xsl:comment/>
		</xsl:element>
	</xsl:template>

</xsl:stylesheet>
