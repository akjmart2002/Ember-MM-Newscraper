﻿' ################################################################################
' #                             EMBER MEDIA MANAGER                              #
' ################################################################################
' ################################################################################
' # This file is part of Ember Media Manager.                                    #
' #                                                                              #
' # Ember Media Manager is free software: you can redistribute it and/or modify  #
' # it under the terms of the GNU General Public License as published by         #
' # the Free Software Foundation, either version 3 of the License, or            #
' # (at your option) any later version.                                          #
' #                                                                              #
' # Ember Media Manager is distributed in the hope that it will be useful,       #
' # but WITHOUT ANY WARRANTY; without even the implied warranty of               #
' # MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                #
' # GNU General Public License for more details.                                 #
' #                                                                              #
' # You should have received a copy of the GNU General Public License            #
' # along with Ember Media Manager.  If not, see <http://www.gnu.org/licenses/>. #
' ###############################################################################

Imports EmberAPI
Imports NLog

Public Class TMDB_Trailer
    Implements Interfaces.ScraperModule_Trailer_Movie


#Region "Fields"
    Shared logger As Logger = LogManager.GetCurrentClassLogger()

    Public Shared ConfigScrapeModifiers As New Structures.ScrapeModifiers
    Public Shared _AssemblyName As String

    ''' <summary>
    ''' Scraping Here
    ''' </summary>
    ''' <remarks></remarks>
    Private strPrivateAPIKey As String = String.Empty
    Private _SpecialSettings As New SpecialSettings
    Private _Name As String = "TMDB_Trailer"
    Private _ScraperEnabled As Boolean = False
    Private _setup As frmSettingsHolder

#End Region 'Fields

#Region "Events"

    Public Event SetupScraperChanged(ByVal name As String, ByVal State As Boolean, ByVal difforder As Integer) Implements Interfaces.ScraperModule_Trailer_Movie.ScraperSetupChanged

#End Region 'Events

#Region "Properties"

    Property ScraperEnabled() As Boolean Implements Interfaces.ScraperModule_Trailer_Movie.ScraperEnabled
        Get
            Return _ScraperEnabled
        End Get
        Set(ByVal value As Boolean)
            _ScraperEnabled = value
        End Set
    End Property

#End Region 'Properties

#Region "Methods"
    Private Sub Handle_ModuleSettingsChanged()
        RaiseEvent ModuleSettingsChanged()
    End Sub

    Private Sub Handle_SetupNeedsRestart()
        RaiseEvent SetupNeedsRestart()
    End Sub

    Private Sub Handle_SetupScraperChanged(ByVal state As Boolean, ByVal difforder As Integer)
        ScraperEnabled = state
        RaiseEvent SetupScraperChanged(String.Concat(Me._Name, "Scraper"), state, difforder)
    End Sub

    Function InjectSetupScraper() As Containers.SettingsPanel Implements Interfaces.ScraperModule_Trailer_Movie.InjectSetupScraper
        Dim Spanel As New Containers.SettingsPanel
        _setup = New frmSettingsHolder
        LoadSettings()
        _setup.chkEnabled.Checked = _ScraperEnabled
        _setup.txtApiKey.Text = strPrivateAPIKey
        _setup.chkFallBackEng.Checked = _SpecialSettings.FallBackEng

        If Not String.IsNullOrEmpty(strPrivateAPIKey) Then
            _setup.btnUnlockAPI.Text = Master.eLang.GetString(443, "Use embedded API Key")
            _setup.lblEMMAPI.Visible = False
            _setup.txtApiKey.Enabled = True
        End If

        _setup.orderChanged()

        Spanel.Name = String.Concat(Me._Name, "Scraper")
        Spanel.Text = "TMDB"
        Spanel.Prefix = "TMDBTrailer_"
        Spanel.Order = 110
        Spanel.Parent = "pnlMovieTrailer"
        Spanel.Type = Master.eLang.GetString(36, "Movies")
        Spanel.ImageIndex = If(Me._ScraperEnabled, 9, 10)
        Spanel.Panel = Me._setup.pnlSettings

        AddHandler _setup.SetupScraperChanged, AddressOf Handle_SetupScraperChanged
        AddHandler _setup.ModuleSettingsChanged, AddressOf Handle_ModuleSettingsChanged
        AddHandler _setup.SetupNeedsRestart, AddressOf Handle_SetupNeedsRestart
        Return Spanel
    End Function

    Sub LoadSettings()

        ConfigScrapeModifiers.MainTrailer = AdvancedSettings.GetBooleanSetting("DoTrailer", True)
        _SpecialSettings.APIKey = If(String.IsNullOrEmpty(strPrivateAPIKey), "44810eefccd9cb1fa1d57e7b0d67b08d", strPrivateAPIKey)
        _SpecialSettings.FallBackEng = AdvancedSettings.GetBooleanSetting("FallBackEn", False)
        strPrivateAPIKey = AdvancedSettings.GetSetting("TMDBAPIKey", "")

    End Sub

    Function Scraper_Movie(ByRef DBMovie As Database.DBElement, ByVal Type As Enums.ModifierType, ByRef TrailerList As List(Of MediaContainers.Trailer)) As Interfaces.ModuleResult
        logger.Trace("[TMDB_Trailer] [Scraper_Movie] [Start]")

        LoadSettings()
        _SpecialSettings.PrefLanguage = DBMovie.Language

        'If String.IsNullOrEmpty(DBMovie.Movie.TMDBID) Then
        '    DBMovie.Movie.TMDBID = ModulesManager.Instance.GetMovieTMDBID(DBMovie.Movie.ID)
        'End If

        If Not String.IsNullOrEmpty(DBMovie.Movie.TMDBID) Then
            Dim _scraper As New TMDB.Scraper(_SpecialSettings)

            TrailerList = _scraper.GetTrailers(DBMovie.Movie.TMDBID)
        End If

        logger.Trace("[TMDB_Trailer] [Scraper_Movie] [Done]")
        Return New Interfaces.ModuleResult With {.breakChain = False}
    End Function

    Sub SaveSettings()
        Using settings = New AdvancedSettings()
            settings.SetSetting("TMDBAPIKey", _setup.txtApiKey.Text)
            settings.SetBooleanSetting("FallBackEn", _SpecialSettings.FallBackEng)
            settings.SetBooleanSetting("DoTrailer", ConfigScrapeModifiers.MainTrailer)
        End Using
    End Sub

    Sub SaveSetupScraper(ByVal DoDispose As Boolean) Implements Interfaces.ScraperModule_Trailer_Movie.SaveSetupScraper
        _SpecialSettings.FallBackEng = _setup.chkFallBackEng.Checked
        SaveSettings()
        'ModulesManager.Instance.SaveSettings()
        If DoDispose Then
            RemoveHandler _setup.SetupScraperChanged, AddressOf Handle_SetupScraperChanged
            RemoveHandler _setup.ModuleSettingsChanged, AddressOf Handle_ModuleSettingsChanged
            RemoveHandler _setup.SetupNeedsRestart, AddressOf Handle_SetupNeedsRestart
            _setup.Dispose()
        End If
    End Sub

    Public Sub ScraperOrderChanged() Implements EmberAPI.Interfaces.ScraperModule_Trailer_Movie.ScraperOrderChanged
        _setup.orderChanged()
    End Sub

#End Region 'Methods

#Region "Nested Types"

    Structure SpecialSettings

#Region "Fields"
        Dim APIKey As String
        Dim PrefLanguage As String
        Dim FallBackEng As Boolean
#End Region 'Fields

    End Structure

#End Region 'Nested Types

End Class