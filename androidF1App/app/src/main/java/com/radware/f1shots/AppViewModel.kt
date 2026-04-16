package com.radware.f1shots

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class AppViewModel : ViewModel() {
    val baseUrl = "http://10.0.2.2:5016"

    var mode by mutableStateOf(AuthMode.Register)
    var email by mutableStateOf("")
    var password by mutableStateOf("")
    var userName by mutableStateOf("")
    var isAuthenticating by mutableStateOf(false)
    var statusMessage by mutableStateOf("Wpisz dane i utwórz konto w lokalnym API.")
    var authResult by mutableStateOf<LoginResponse?>(null)
    var registerResult by mutableStateOf<RegisterResponse?>(null)
    var session by mutableStateOf<UserSession?>(null)

    var currentUser by mutableStateOf<MeResponse?>(null)
    val myGroups = mutableStateListOf<MyGroupResponse>()
    val publicGroups = mutableStateListOf<PublicGroupResponse>()
    var selectedGroupDetails by mutableStateOf<GroupDetailsResponse?>(null)
    val selectedGroupMembers = mutableStateListOf<GroupMemberResponse>()
    val latestJoinCodeByGroupId = mutableStateMapOf<String, String>()

    var createGroupName by mutableStateOf("")
    var createVisibility by mutableStateOf(GroupVisibility.Public)
    var joinCode by mutableStateOf("")
    var updateGroupName by mutableStateOf("")
    var updateVisibility by mutableStateOf(GroupVisibility.Public)

    var isLoadingHome by mutableStateOf(false)
    var homeStatusMessage by mutableStateOf("Po zalogowaniu odświeżymy dane użytkownika i grup.")

    val canSubmitAuth: Boolean
        get() {
            val common = email.trim().isNotEmpty() && password.isNotEmpty()
            return if (mode == AuthMode.Register) common && userName.trim().isNotEmpty() else common
        }

    val statusIcon: String
        get() = when {
            isAuthenticating -> "hourglass"
            authResult != null || registerResult != null -> "check"
            else -> "info"
        }

    fun submitAuth() {
        if (!canSubmitAuth) return

        viewModelScope.launch {
            isAuthenticating = true
            authResult = null
            registerResult = null

            try {
                when (mode) {
                    AuthMode.Register -> {
                        val response = withContext(Dispatchers.IO) {
                            ApiClient(baseUrl).post<RegisterRequest, RegisterResponse>(
                                path = "/api/Auth/register",
                                body = RegisterRequest(
                                    email = email.trim(),
                                    userName = userName.trim(),
                                    password = password
                                )
                            )
                        }
                        registerResult = response
                        statusMessage = "Rejestracja zakończona powodzeniem. Teraz możesz przełączyć się na logowanie."
                    }

                    AuthMode.Login -> {
                        val request = LoginRequest(email = email.trim(), password = password)
                        val response = withContext(Dispatchers.IO) {
                            ApiClient(baseUrl).post<LoginRequest, LoginResponse>(
                                path = "/api/Auth/login",
                                body = request
                            )
                        }
                        authResult = response
                        session = UserSession(
                            email = request.email,
                            accessToken = response.accessToken,
                            refreshToken = response.refreshToken,
                            accessTokenExpiresAtUtc = response.accessTokenExpiresAtUtc
                        )
                        statusMessage = "Logowanie zakończone powodzeniem."
                        refreshHome()
                    }
                }
            } catch (exception: Exception) {
                statusMessage = exception.message ?: "Wystąpił nieznany błąd."
            } finally {
                isAuthenticating = false
            }
        }
    }

    fun refreshHome() {
        val activeSession = session ?: return

        viewModelScope.launch {
            isLoadingHome = true
            try {
                val client = ApiClient(baseUrl, activeSession.accessToken)
                val me = withContext(Dispatchers.IO) { client.get<MeResponse>("/api/User/me") }
                val mine = withContext(Dispatchers.IO) { client.get<Array<MyGroupResponse>>("/api/Groups/mine") }
                val publicList = withContext(Dispatchers.IO) { client.get<Array<PublicGroupResponse>>("/api/Groups/public") }

                currentUser = me
                myGroups.replaceAll(mine.toList())
                publicGroups.replaceAll(publicList.toList())
                homeStatusMessage = "Dane zostały odświeżone."

                selectedGroupDetails?.id?.let { loadSelectedGroup(it) }
            } catch (exception: Exception) {
                homeStatusMessage = exception.message ?: "Nie udało się pobrać danych."
            } finally {
                isLoadingHome = false
            }
        }
    }

    fun createGroup() {
        val activeSession = session ?: return
        val name = createGroupName.trim()
        if (name.isEmpty()) return

        viewModelScope.launch {
            isLoadingHome = true
            try {
                val response = withContext(Dispatchers.IO) {
                    ApiClient(baseUrl, activeSession.accessToken).post<CreateGroupRequest, CreateGroupResponse>(
                        path = "/api/Groups",
                        body = CreateGroupRequest(name = name, visibility = createVisibility)
                    )
                }
                latestJoinCodeByGroupId[response.id] = response.joinCode
                createGroupName = ""
                updateGroupName = response.name
                updateVisibility = response.visibility
                homeStatusMessage = "Grupa ${response.name} została utworzona."
                refreshHome()
                loadSelectedGroup(response.id)
            } catch (exception: Exception) {
                homeStatusMessage = exception.message ?: "Nie udało się utworzyć grupy."
            } finally {
                isLoadingHome = false
            }
        }
    }

    fun joinGroup() {
        val activeSession = session ?: return
        val code = joinCode.trim().uppercase()
        if (code.isEmpty()) return

        viewModelScope.launch {
            isLoadingHome = true
            try {
                val response = withContext(Dispatchers.IO) {
                    ApiClient(baseUrl, activeSession.accessToken).post<JoinGroupRequest, JoinGroupResponse>(
                        path = "/api/Groups/00000000-0000-0000-0000-000000000000/join",
                        body = JoinGroupRequest(joinCode = code)
                    )
                }
                joinCode = ""
                homeStatusMessage = "Dołączono do grupy."
                refreshHome()
                loadSelectedGroup(response.groupId)
            } catch (exception: Exception) {
                homeStatusMessage = exception.message ?: "Nie udało się dołączyć do grupy."
            } finally {
                isLoadingHome = false
            }
        }
    }

    fun selectGroup(groupId: String) {
        viewModelScope.launch {
            loadSelectedGroup(groupId)
        }
    }

    fun updateSelectedGroup() {
        val activeSession = session ?: return
        val groupId = selectedGroupDetails?.id ?: return

        viewModelScope.launch {
            isLoadingHome = true
            try {
                val response = withContext(Dispatchers.IO) {
                    ApiClient(baseUrl, activeSession.accessToken).patch<UpdateGroupRequest, UpdateGroupResponse>(
                        path = "/api/Groups/$groupId",
                        body = UpdateGroupRequest(
                            name = updateGroupName.trim().ifEmpty { null },
                            visibility = updateVisibility
                        )
                    )
                }
                latestJoinCodeByGroupId[response.id] = response.joinCode
                homeStatusMessage = "Zmiany grupy zostały zapisane."
                refreshHome()
                loadSelectedGroup(response.id)
            } catch (exception: Exception) {
                homeStatusMessage = exception.message ?: "Nie udało się zaktualizować grupy."
            } finally {
                isLoadingHome = false
            }
        }
    }

    fun leaveSelectedGroup() {
        val activeSession = session ?: return
        val groupId = selectedGroupDetails?.id ?: return

        viewModelScope.launch {
            isLoadingHome = true
            try {
                withContext(Dispatchers.IO) {
                    ApiClient(baseUrl, activeSession.accessToken).postWithoutResponse("/api/Groups/$groupId/leave")
                }
                selectedGroupDetails = null
                selectedGroupMembers.clear()
                homeStatusMessage = "Opuściłeś grupę."
                refreshHome()
            } catch (exception: Exception) {
                homeStatusMessage = exception.message ?: "Nie udało się opuścić grupy."
            } finally {
                isLoadingHome = false
            }
        }
    }

    fun logout() {
        session = null
        authResult = null
        registerResult = null
        currentUser = null
        myGroups.clear()
        publicGroups.clear()
        selectedGroupDetails = null
        selectedGroupMembers.clear()
        latestJoinCodeByGroupId.clear()
        password = ""
        homeStatusMessage = "Sesja została wyczyszczona lokalnie."
        statusMessage = "Sesja została wyczyszczona lokalnie."
    }

    private suspend fun loadSelectedGroup(groupId: String) {
        val activeSession = session ?: return

        try {
            val client = ApiClient(baseUrl, activeSession.accessToken)
            val details = withContext(Dispatchers.IO) {
                client.get<GroupDetailsResponse>("/api/Groups/$groupId")
            }

            selectedGroupDetails = details
            updateGroupName = details.name
            updateVisibility = details.visibility

            if (details.isMember) {
                val members = withContext(Dispatchers.IO) {
                    client.get<Array<GroupMemberResponse>>("/api/Groups/$groupId/members")
                }
                selectedGroupMembers.replaceAll(members.toList())
            } else {
                selectedGroupMembers.clear()
            }
        } catch (exception: Exception) {
            homeStatusMessage = exception.message ?: "Nie udało się pobrać szczegółów grupy."
        }
    }

    private fun <T> MutableList<T>.replaceAll(items: List<T>) {
        clear()
        addAll(items)
    }
}
