<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
								exclude-result-prefixes="msxsl"
								xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
								xmlns:msxsl="urn:schemas-microsoft-com:xslt"
								xmlns:xhtml="http://www.w3.org/1999/xhtml"
								xmlns:mtps="http://msdn2.microsoft.com/mtps"
								xmlns:cs="urn:code-snippet"
>
	<!-- ============================================================================================
	Parameters

	These parameters are used to remove elements from pre-formatted code snippets when making the
	plain copy used for Copy and Print.
	============================================================================================= -->

	<xsl:param name="plain-remove-element"></xsl:param>
	<xsl:param name="plain-remove-id"></xsl:param>
	<xsl:param name="plain-remove-class"></xsl:param>

	<!-- ============================================================================================
	Override code grouping to consistently use the 'isMajorLanguage' template and to better handle
	multiple groups.

	Snippets are rendered as a group only when:
	- They are descendants of mtps:CollapsibleArea or children of a div whose id starts with 'snippetGroup'
	- They are contiguous (no intervening elements that are not snippets)
	- They are unique (duplicates are split into groups)
	- They pass the 'isMajorLanguage' test 
	============================================================================================= -->

	<xsl:template match="mtps:CodeSnippet"
								priority ="2"
								name="codeSnippetOverride">
		<xsl:choose>
			<xsl:when test="ancestor::mtps:CollapsibleArea[count(descendant::mtps:CodeSnippet) > 1] or parent::xhtml:div[starts-with(@id,'snippetGroup') and (count(mtps:CodeSnippet) > 1)]">
				<xsl:variable name="v_currentId">
					<xsl:value-of select="generate-id(.)"/>
				</xsl:variable>
				<xsl:variable name="v_prevPosition">
					<xsl:for-each select="parent::*/child::*">
						<xsl:if test="generate-id(.)=$v_currentId">
							<xsl:number value="position()-1"/>
						</xsl:if>
					</xsl:for-each>
				</xsl:variable>

				<xsl:choose>
					<xsl:when test="name(parent::*/child::*[position()=$v_prevPosition])=name()">
						<!--<xsl:comment xml:space="preserve">skip [<xsl:value-of select="$v_prevPosition + 1" />] [<xsl:value-of select="@Language" />] [<xsl:value-of select="@DisplayLanguage" />]</xsl:comment>-->
					</xsl:when>
					<xsl:when test="following-sibling::*[1]/self::mtps:CodeSnippet">
						<xsl:call-template name="codeSnippetGroup">
							<xsl:with-param name="codeSnippets"
															select=". | following-sibling::mtps:CodeSnippet[not(preceding-sibling::*[generate-id(.)=generate-id((current()/following-sibling::*[not(self::mtps:CodeSnippet)])[1])])]"/>
						</xsl:call-template>
					</xsl:when>
					<xsl:otherwise>
						<!--<xsl:comment xml:space="preserve">standalonesnippet(a) [<xsl:value-of select="@Language" />] [<xsl:value-of select="@DisplayLanguage" />]</xsl:comment>-->
						<xsl:call-template name="standalonesnippet"/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<!--<xsl:comment xml:space="preserve">standalonesnippet(b) [<xsl:value-of select="@Language" />] [<xsl:value-of select="@DisplayLanguage" />]</xsl:comment>-->
				<xsl:call-template name="standalonesnippet"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- ============================================================================================
	This template receives a contiguous set of snippets and splits any duplicates into separate sets.
	============================================================================================= -->
	<xsl:template name="codeSnippetGroup">
		<xsl:param name="codeSnippets"/>
		<xsl:param name="codeSnippetCount"
							 select="count($codeSnippets)"/>

		<!--<xsl:variable name="v_languages">
			<xsl:for-each select="$codeSnippets">
				<xsl:value-of select="concat(' [',@Language,'][',@DisplayLanguage,']')" />
			</xsl:for-each>
		</xsl:variable>
		<xsl:comment xml:space="preserve">codeSnippetGroup [<xsl:value-of select="$codeSnippetCount" />]<xsl:value-of select="$v_languages" /></xsl:comment>-->

		<xsl:choose>
			<xsl:when test="$codeSnippetCount = 1">
				<xsl:for-each select="$codeSnippets">
					<!--<xsl:comment xml:space="preserve">standalonesnippet(c) [<xsl:value-of select="@Language" />] [<xsl:value-of select="@DisplayLanguage" />]</xsl:comment>-->
					<xsl:call-template name="standalonesnippet"/>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>

				<!-- Must use a copy of the snippets for this check so that preceding-sibling applies only to THESE snippets. -->
				<!-- Otherwise, preceding-sibling will apply to all snippets with the same parent in the source document. -->
				<xsl:variable name="v_snippetsCopy">
					<xsl:copy-of select="$codeSnippets"/>
				</xsl:variable>
				<xsl:variable name="v_duplicates">
					<xsl:for-each select="msxsl:node-set($v_snippetsCopy)/*">
						<xsl:if test="preceding-sibling::*[@Language = current()/@Language]">
							<xsl:value-of select="concat(position(),';')"/>
						</xsl:if>
					</xsl:for-each>
				</xsl:variable>

				<xsl:choose>
					<xsl:when test="string($v_duplicates)=''">
						<xsl:call-template name="codeSnippetGroupUnique">
							<xsl:with-param name="codeSnippets"
															select="$codeSnippets"/>
							<xsl:with-param name="codeSnippetCount"
															select="$codeSnippetCount"/>
						</xsl:call-template>
					</xsl:when>
					<xsl:otherwise>
						<xsl:variable name="v_dupPosition">
							<xsl:value-of select="substring-before(string($v_duplicates),';')"/>
						</xsl:variable>
						<xsl:call-template name="codeSnippetGroup">
							<xsl:with-param name="codeSnippets"
															select="$codeSnippets[position() &lt; $v_dupPosition]"/>
						</xsl:call-template>
						<xsl:call-template name="codeSnippetGroup">
							<xsl:with-param name="codeSnippets"
															select="$codeSnippets[not(position() &lt; $v_dupPosition)]"/>
						</xsl:call-template>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- ============================================================================================
	This template receives a contiguous, unique set of snippets and checks for 'isMajorLanguage'.
	- If all snippets are for a major language, they are rendered as a group
	- If all snippets are NOT for a major language, they are rendered standalone
	- Otherwise the snippets are separated into consistent sets
	============================================================================================= -->
	<xsl:template name="codeSnippetGroupUnique">
		<xsl:param name="codeSnippets"/>
		<xsl:param name="codeSnippetCount"
							 select="count($codeSnippets)"/>

		<!--<xsl:variable name="v_languages">
			<xsl:for-each select="$codeSnippets">
				<xsl:value-of select="concat(' [',@Language,'][',@DisplayLanguage,']')" />
			</xsl:for-each>
		</xsl:variable>
		<xsl:comment xml:space="preserve">codeSnippetGroupUnique [<xsl:value-of select="$codeSnippetCount" />]<xsl:value-of select="$v_languages" /></xsl:comment>-->

		<xsl:choose>
			<xsl:when test="$codeSnippetCount = 1">
				<xsl:for-each select="$codeSnippets">
					<!--<xsl:comment xml:space="preserve">standalonesnippet(d) [<xsl:value-of select="@Language" />] [<xsl:value-of select="@DisplayLanguage" />]</xsl:comment>-->
					<xsl:call-template name="standalonesnippet"/>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="v_allMajorLanguage">
					<xsl:for-each select="$codeSnippets">
						<xsl:variable name="v_isMajorLanguage">
							<xsl:call-template name="isMajorLanguage">
								<xsl:with-param name="lang"
																select="@DisplayLanguage"/>
							</xsl:call-template>
						</xsl:variable>
						<value>
							<xsl:choose>
								<xsl:when test="$v_isMajorLanguage='true'">
									<xsl:value-of select="'true'"/>
								</xsl:when>
								<xsl:otherwise>
									<xsl:value-of select="'false'"/>
								</xsl:otherwise>
							</xsl:choose>
						</value>
					</xsl:for-each>
				</xsl:variable>
				<xsl:variable name="v_allMajorLanguageSame">
					<xsl:for-each select="msxsl:node-set($v_allMajorLanguage)/*">
						<xsl:if test="preceding-sibling::*[text() != current()/text()]">
							<xsl:value-of select="concat(position(),';')"/>
						</xsl:if>
					</xsl:for-each>
				</xsl:variable>

				<xsl:choose>
					<xsl:when test="string($v_allMajorLanguageSame)='' and not(contains($v_allMajorLanguage,'false'))">
						<!--<xsl:comment xml:space="preserve">renderSnippet [<xsl:value-of select="$codeSnippetCount"/>]</xsl:comment>
						<xsl:for-each select="$codeSnippets">
							<xsl:comment xml:space="preserve">  [<xsl:value-of select="@Language" />] [<xsl:value-of select="@DisplayLanguage" />]</xsl:comment>
						</xsl:for-each>-->
						<xsl:call-template name="renderSnippet">
							<xsl:with-param name="snippetCount"
															select="$codeSnippetCount"/>
							<xsl:with-param name="snippets"
															select="$codeSnippets"/>
							<xsl:with-param name="showLanTabs"
															select="true()" />
							<xsl:with-param name="unrecognized"
															select="'false'" />
						</xsl:call-template>
					</xsl:when>
					<xsl:when test="string($v_allMajorLanguageSame)='' and contains($v_allMajorLanguage,'false')">
						<xsl:for-each select="$codeSnippets">
							<!--<xsl:comment xml:space="preserve">standalonesnippet(e) [<xsl:value-of select="@Language" />] [<xsl:value-of select="@DisplayLanguage" />]</xsl:comment>-->
							<xsl:call-template name="standalonesnippet"/>
						</xsl:for-each>
					</xsl:when>
					<xsl:otherwise>
						<xsl:variable name="v_dupPosition">
							<xsl:value-of select="substring-before(string($v_allMajorLanguageSame),';')"/>
						</xsl:variable>
						<xsl:call-template name="codeSnippetGroupUnique">
							<xsl:with-param name="codeSnippets"
															select="$codeSnippets[position() &lt; $v_dupPosition]"/>
						</xsl:call-template>
						<xsl:call-template name="codeSnippetGroupUnique">
							<xsl:with-param name="codeSnippets"
															select="$codeSnippets[not(position() &lt; $v_dupPosition)]"/>
						</xsl:call-template>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- ============================================================================================
	Override code formatting to allow for pre-formatted code
	============================================================================================= -->

	<xsl:template name="renderCodeDiv">
		<xsl:param name="id"/>
		<xsl:param name="pos"/>
		<xsl:param name="uniqueLangIndex"
							 select="1"/>
		<xsl:param name="lang"/>
		<xsl:param name="plainCode"
							 select="'false'"/>
		<xsl:param name="snippetCode"/>
		<xsl:param name="unrecognized"
							 select="'false'"/>
		<xsl:param name="ContainsMarkup" />
		<!--<xsl:comment xml:space="preserve">renderCodeDiv [<xsl:value-of select="$id" />] [<xsl:value-of select="$pos" />] [<xsl:value-of select="$uniqueLangIndex" />] [<xsl:value-of select="$lang" />] [<xsl:value-of select="$unrecognized" />] [<xsl:value-of select="$ContainsMarkup" />]</xsl:comment>-->
		<xsl:element name="div"
								 namespace="{$xhtml}">
			<xsl:attribute name="id">
				<xsl:value-of select="$id"/>
			</xsl:attribute>
			<xsl:attribute name="class">OH_CodeSnippetContainerCode</xsl:attribute>
			<xsl:attribute name="style">
				<xsl:choose>
					<xsl:when test="$plainCode='true'">
						<xsl:text>display: none</xsl:text>
					</xsl:when>
					<xsl:when test="$uniqueLangIndex=1">
						<xsl:text>display: block</xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>display: none</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
			<xsl:variable name="spacedSnippetCode">
				<xsl:apply-templates select="msxsl:node-set($snippetCode)/node()"
														 mode="codeSpacing"/>
			</xsl:variable>
			<xsl:choose>
				<!-- If the snippet contains any elements, it's pre-formatted -->
				<xsl:when test="msxsl:node-set($spacedSnippetCode)/*">
					<xsl:choose>
						<xsl:when test="$plainCode='true'">
							<xsl:element name="pre"
													 namespace="{$xhtml}">
								<xsl:choose>
									<xsl:when test="$plain-remove-element!='' or $plain-remove-id!='' or $plain-remove-class!=''">
										<xsl:variable name="plainSnippetCode">
											<xsl:apply-templates select="msxsl:node-set($spacedSnippetCode)"
																					 mode="plainCode"/>
										</xsl:variable>
										<xsl:value-of select="cs:plainCode($plainSnippetCode)"
																	disable-output-escaping="yes"/>
									</xsl:when>
									<xsl:otherwise>
										<xsl:value-of select="cs:plainCode($spacedSnippetCode)"
																	disable-output-escaping="yes"/>
									</xsl:otherwise>
								</xsl:choose>
							</xsl:element>
						</xsl:when>
						<xsl:otherwise>
							<xsl:element name="pre"
													 namespace="{$xhtml}">
								<xsl:attribute name="style">word-wrap:normal;</xsl:attribute>
								<xsl:copy-of select="$spacedSnippetCode"/>
							</xsl:element>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:when>
				<!-- Otherwise it's only text -->
				<xsl:otherwise>
					<xsl:choose>
						<xsl:when test="$plainCode='true'">
							<xsl:element name="pre"
													 namespace="{$xhtml}">
								<xsl:value-of select="cs:ConvertWhiteSpace(cs:plainCode($snippetCode))"
															disable-output-escaping="yes"/>
							</xsl:element>
						</xsl:when>
						<xsl:when test="$ContainsMarkup='true'">
							<xsl:element name="pre"
													 namespace="{$xhtml}">
								<xsl:copy-of select="cs:ConvertWhiteSpace($snippetCode)"/>
							</xsl:element>
						</xsl:when>
						<xsl:otherwise>
							<xsl:element name="pre"
													 namespace="{$xhtml}">
								<xsl:value-of select="cs:ConvertWhiteSpace(cs:test($snippetCode, $lang, 'en-us'))"
															disable-output-escaping="yes"/>
							</xsl:element>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:element>
	</xsl:template>

	<!-- ======================================================================================== -->

	<xsl:template match="*"
								mode="codeSpacing"
								name="codeSpacingElement">
		<xsl:copy>
			<xsl:apply-templates select="@*"/>
			<xsl:apply-templates mode="codeSpacing"/>
			<xsl:if test="not(node()) and not(self::xhtml:br) and not(self::xhtml:hr)">
				<xsl:value-of select="''"/>
			</xsl:if>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="xhtml:pre"
								mode="codeSpacing"
								name="codeSpacingContainer">
		<xsl:apply-templates mode="codeSpacing"/>
	</xsl:template>

	<xsl:template match="text()"
								mode="codeSpacing"
								name="codeSpacingText">
		<xsl:choose>
			<xsl:when test=".=' ' or .='&#160;'">
				<xsl:value-of select="'&#160;'"/>
			</xsl:when>
			<xsl:when test="normalize-space(.)='' and contains(.,'&#10;')">
				<xsl:value-of select="concat('&#160;','&#10;',substring-after(translate(.,' &#13;','&#160;'),'&#10;'))"/>
			</xsl:when>
			<xsl:when test="normalize-space(.)='' and contains(.,'&#13;')">
				<xsl:value-of select="concat('&#160;','&#10;',substring-after(translate(.,' ','&#160;'),'&#13;'))"/>
			</xsl:when>
			<xsl:when test=".!='' and normalize-space(.)=''">
				<xsl:value-of select="translate(.,' ','&#160;')"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="."/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- ======================================================================================== -->

	<xsl:template match="*"
								mode="plainCode"
								name="plainCodeElement">
		<xsl:choose>
			<xsl:when test="contains($plain-remove-element,name()) or (@id and contains($plain-remove-id,@id)) or (@class and contains($plain-remove-class,@class))">
				<!--<xsl:comment xml:space="preserve">skip[<xsl:value-of select="."/>]</xsl:comment>-->
			</xsl:when>
			<xsl:otherwise>
				<xsl:copy>
					<xsl:apply-templates select="@*"/>
					<xsl:apply-templates mode="plainCode"/>
				</xsl:copy>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="text()"
								mode="plainCode"
								name="plainCodeText">
		<xsl:call-template name="codeSpacingText"/>
	</xsl:template>

	<!-- ============================================================================================
	Accumulate default tabs set
	============================================================================================= -->

	<!-- To customize the starter set of grouped code languages, update this list -->
	<xsl:variable name="groupedLanguages">
		<value>Visual Basic</value>
		<value>C#</value>
		<value>Visual C++</value>
		<value>F#</value>
		<value>JScript</value>
	</xsl:variable>
	<!-- To customize the code languages that should NOT be grouped, update this list -->
	<xsl:variable name="separateLanguages">
		<value>J#</value>
		<value>JavaScript</value>
		<value>XML</value>
		<value>XAML</value>
		<value>HTML</value>
		<value>ASP.NET</value>
	</xsl:variable>

	<xsl:variable name="devLanguages">
		<xsl:for-each select="/xhtml:html/xhtml:head/xhtml:xml/xhtml:list[@id='BrandingLanguages']/xhtml:value">
			<value>
				<xsl:value-of select="text()"/>
			</value>
		</xsl:for-each>
	</xsl:variable>
	<xsl:variable name="syntaxLanguages">
		<xsl:for-each select="/xhtml:html/xhtml:head/xhtml:xml/xhtml:list[@id='BrandingSyntaxLanguages']/xhtml:value">
			<value>
				<xsl:value-of select="text()"/>
			</value>
		</xsl:for-each>
	</xsl:variable>

	<xsl:variable name="uniqueLangTabsSet">
		<xsl:for-each select="msxsl:node-set($groupedLanguages)/value">
			<xsl:choose>
				<xsl:when test="msxsl:node-set($separateLanguages)/value[current()/text()=text()]"/>
				<xsl:otherwise>
					<value>
						<xsl:value-of select="text()"/>
					</value>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:for-each>
		<xsl:for-each select="msxsl:node-set($devLanguages)/value">
			<xsl:choose>
				<xsl:when test="msxsl:node-set($separateLanguages)/value[current()/text()=text()]"/>
				<xsl:when test="msxsl:node-set($groupedLanguages)/value[current()/text()=text()]"/>
				<xsl:otherwise>
					<value>
						<xsl:value-of select="text()"/>
					</value>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:for-each>
		<xsl:for-each select="msxsl:node-set($syntaxLanguages)/value">
			<xsl:choose>
				<xsl:when test="msxsl:node-set($separateLanguages)/value[current()/text()=text()]"/>
				<xsl:when test="msxsl:node-set($groupedLanguages)/value[current()/text()=text()]"/>
				<xsl:when test="msxsl:node-set($devLanguages)/value[current()/text()=text()]"/>
				<xsl:otherwise>
					<value>
						<xsl:value-of select="text()"/>
					</value>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:for-each>
	</xsl:variable>
	<xsl:variable name="uniqueLangTabsSetCount"
								select="count(msxsl:node-set($uniqueLangTabsSet)/value)"/>

	<xsl:template match="/xhtml:html/xhtml:head/xhtml:xml/xhtml:list[@id='BrandingLanguages']"/>
	<xsl:template match="/xhtml:html/xhtml:head/xhtml:xml/xhtml:list[@id='BrandingSyntaxLanguages']"/>

	<!-- ============================================================================================
	Override of isMajorLanguage

	The default implementation of this template uses the "contains" function which means that if
	the display language "contains" one of the major language names it matches - even if it's not 
	actually the same.  The intention is to match Visual Basic, Visual Basic Declaration and 
	Visual Basic Usage, but it is applied to0 broadly.  This implementation is more accurate.
	============================================================================================= -->

	<xsl:template name="isMajorLanguage">
		<xsl:param name="lang"/>
		<xsl:for-each select="msxsl:node-set($uniqueLangTabsSet)/value">
			<xsl:choose>
				<xsl:when test="$lang=.">
					<xsl:value-of select="'true'"/>
				</xsl:when>
				<xsl:when test="contains($lang,.)">
					<xsl:variable name="loweredLang"
												select="translate($lang, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')" />
					<xsl:if test="contains($loweredLang,'declaration') or contains($loweredLang,'usage')">
						<xsl:value-of select="'true'"/>
					</xsl:if>
				</xsl:when>
			</xsl:choose>
		</xsl:for-each>
	</xsl:template>

</xsl:stylesheet>
