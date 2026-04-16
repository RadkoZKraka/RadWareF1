import Foundation
import Observation

@MainActor
@Observable
final class AppViewModel {
    let baseURLString = "http://localhost:5016"

    var mode: AuthMode = .register {
        didSet {
            statusMessage = mode == .register
                ? "Wpisz dane i utwórz konto w lokalnym API."
                : "Wpisz dane istniejącego konta i pobierz token JWT."
            registerResult = nil
            authResult = nil
        }
    }

    var email = ""
    var password = ""
    var userName = ""
    var isAuthenticating = false
    var statusMessage = "Wpisz dane i utwórz konto w lokalnym API."
    var authResult: LoginResponse?
    var registerResult: RegisterResponse?
    var session: UserSession?

    var currentUser: MeResponse?
    var myGroups: [MyGroupResponse] = []
    var publicGroups: [PublicGroupResponse] = []
    var selectedGroupDetails: GroupDetailsResponse?
    var selectedGroupMembers: [GroupMemberResponse] = []
    var latestJoinCodeByGroupId: [UUID: String] = [:]

    var createGroupName = ""
    var createVisibility: GroupVisibility = .public
    var joinCode = ""
    var updateGroupName = ""
    var updateVisibility: GroupVisibility = .public

    var isLoadingHome = false
    var homeStatusMessage = "Po zalogowaniu odświeżymy dane użytkownika i grup."

    var canSubmitAuth: Bool {
        let commonFieldsFilled = !email.trimmed.isEmpty && !password.isEmpty

        if mode == .register {
            return commonFieldsFilled && !userName.trimmed.isEmpty
        }

        return commonFieldsFilled
    }

    var statusIcon: String {
        if isAuthenticating { return "hourglass" }
        if authResult != nil || registerResult != nil { return "checkmark.circle.fill" }
        return "info.circle"
    }

    var homeStatusIcon: String {
        isLoadingHome ? "hourglass" : "flag.checkered.circle"
    }

    func submitAuth() async {
        guard canSubmitAuth else { return }

        isAuthenticating = true
        authResult = nil
        registerResult = nil

        do {
            switch mode {
            case .register:
                let request = RegisterRequest(
                    email: email.trimmed,
                    userName: userName.trimmed,
                    password: password
                )
                let response: RegisterResponse = try await APIClient(baseURLString: baseURLString)
                    .post(path: "/api/Auth/register", body: request)
                registerResult = response
                statusMessage = "Rejestracja zakończona powodzeniem. Teraz możesz przełączyć się na logowanie."

            case .login:
                let request = LoginRequest(
                    email: email.trimmed,
                    password: password
                )
                let response: LoginResponse = try await APIClient(baseURLString: baseURLString)
                    .post(path: "/api/Auth/login", body: request)
                authResult = response
                session = UserSession(
                    email: request.email,
                    accessToken: response.accessToken,
                    refreshToken: response.refreshToken,
                    accessTokenExpiresAtUtc: response.accessTokenExpiresAtUtc
                )
                statusMessage = "Logowanie zakończone powodzeniem."
                await refreshHome()
            }
        } catch {
            statusMessage = APIError.describe(error)
        }

        isAuthenticating = false
    }

    func refreshHome() async {
        guard let session else { return }

        isLoadingHome = true

        do {
            let client = APIClient(baseURLString: baseURLString, accessToken: session.accessToken)
            let me: MeResponse = try await client.get(path: "/api/User/me")
            let mine: [MyGroupResponse] = try await client.get(path: "/api/Groups/mine")
            let publicGroups: [PublicGroupResponse] = try await client.get(path: "/api/Groups/public")

            currentUser = me
            myGroups = mine
            self.publicGroups = publicGroups
            homeStatusMessage = "Dane zostały odświeżone."

            if let selectedId = selectedGroupDetails?.id {
                await loadSelectedGroup(id: selectedId)
            }
        } catch {
            homeStatusMessage = APIError.describe(error)
        }

        isLoadingHome = false
    }

    func createGroup() async {
        guard let session else { return }

        let name = createGroupName.trimmed
        guard !name.isEmpty else { return }

        isLoadingHome = true

        do {
            let client = APIClient(baseURLString: baseURLString, accessToken: session.accessToken)
            let request = CreateGroupRequest(name: name, visibility: createVisibility)
            let response: CreateGroupResponse = try await client.post(path: "/api/Groups", body: request)

            latestJoinCodeByGroupId[response.id] = response.joinCode
            createGroupName = ""
            updateGroupName = response.name
            updateVisibility = response.visibility
            homeStatusMessage = "Grupa \(response.name) została utworzona."

            await refreshHome()
            await loadSelectedGroup(id: response.id)
        } catch {
            homeStatusMessage = APIError.describe(error)
        }

        isLoadingHome = false
    }

    func joinGroup() async {
        guard let session else { return }

        let code = joinCode.trimmed.uppercased()
        guard !code.isEmpty else { return }

        isLoadingHome = true

        do {
            let client = APIClient(baseURLString: baseURLString, accessToken: session.accessToken)
            let request = JoinGroupRequest(joinCode: code)
            let response: JoinGroupResponse = try await client.post(
                path: "/api/Groups/00000000-0000-0000-0000-000000000000/join",
                body: request
            )

            joinCode = ""
            homeStatusMessage = "Dołączono do grupy."
            await refreshHome()
            await loadSelectedGroup(id: response.groupId)
        } catch {
            homeStatusMessage = APIError.describe(error)
        }

        isLoadingHome = false
    }

    func selectGroup(_ groupId: UUID) async {
        await loadSelectedGroup(id: groupId)
    }

    func updateSelectedGroup() async {
        guard let session, let selectedGroupDetails else { return }

        isLoadingHome = true

        do {
            let client = APIClient(baseURLString: baseURLString, accessToken: session.accessToken)
            let request = UpdateGroupRequest(
                name: updateGroupName.trimmed.isEmpty ? nil : updateGroupName.trimmed,
                visibility: updateVisibility
            )
            let response: UpdateGroupResponse = try await client.patch(
                path: "/api/Groups/\(selectedGroupDetails.id.uuidString)",
                body: request
            )

            latestJoinCodeByGroupId[response.id] = response.joinCode
            homeStatusMessage = "Zmiany grupy zostały zapisane."
            await refreshHome()
            await loadSelectedGroup(id: response.id)
        } catch {
            homeStatusMessage = APIError.describe(error)
        }

        isLoadingHome = false
    }

    func leaveSelectedGroup() async {
        guard let session, let selectedGroupDetails else { return }

        isLoadingHome = true

        do {
            let client = APIClient(baseURLString: baseURLString, accessToken: session.accessToken)
            try await client.postWithoutResponse(path: "/api/Groups/\(selectedGroupDetails.id.uuidString)/leave")
            homeStatusMessage = "Opuściłeś grupę."
            self.selectedGroupDetails = nil
            self.selectedGroupMembers = []
            await refreshHome()
        } catch {
            homeStatusMessage = APIError.describe(error)
        }

        isLoadingHome = false
    }

    func logout() {
        session = nil
        authResult = nil
        registerResult = nil
        currentUser = nil
        myGroups = []
        publicGroups = []
        selectedGroupDetails = nil
        selectedGroupMembers = []
        latestJoinCodeByGroupId = [:]
        password = ""
        homeStatusMessage = "Sesja została wyczyszczona lokalnie."
        statusMessage = "Sesja została wyczyszczona lokalnie."
    }

    private func loadSelectedGroup(id: UUID) async {
        guard let session else { return }

        do {
            let client = APIClient(baseURLString: baseURLString, accessToken: session.accessToken)
            let details: GroupDetailsResponse = try await client.get(path: "/api/Groups/\(id.uuidString)")

            selectedGroupDetails = details
            updateGroupName = details.name
            updateVisibility = details.visibility

            if details.isMember {
                let members: [GroupMemberResponse] = try await client.get(path: "/api/Groups/\(id.uuidString)/members")
                selectedGroupMembers = members
            } else {
                selectedGroupMembers = []
            }
        } catch {
            homeStatusMessage = APIError.describe(error)
        }
    }
}
