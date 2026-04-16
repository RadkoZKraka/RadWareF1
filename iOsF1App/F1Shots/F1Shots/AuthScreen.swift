import SwiftUI

struct AuthScreen: View {
    @Bindable var viewModel: AppViewModel

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 24) {
                VStack(alignment: .leading, spacing: 8) {
                    Text("Auth playground")
                        .font(.largeTitle.bold())
                    Text("Przetestuj lokalnie rejestrację i logowanie do backendu RadWareF1.")
                        .foregroundStyle(.secondary)
                    Text("API: \(viewModel.baseURLString)")
                        .font(.footnote.monospaced())
                        .foregroundStyle(.secondary)
                }

                Picker("Tryb", selection: $viewModel.mode) {
                    ForEach(AuthMode.allCases) { mode in
                        Text(mode.title).tag(mode)
                    }
                }
                .pickerStyle(.segmented)

                VStack(spacing: 14) {
                    if viewModel.mode == .register {
                        TextField("User name", text: $viewModel.userName)
                            .textInputAutocapitalization(.never)
                            .autocorrectionDisabled()
                            .textFieldStyle(.roundedBorder)
                    }

                    TextField("Email", text: $viewModel.email)
                        .keyboardType(.emailAddress)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                        .textFieldStyle(.roundedBorder)

                    SecureField("Password", text: $viewModel.password)
                        .textFieldStyle(.roundedBorder)
                }
                .padding(18)
                .background(.thinMaterial, in: RoundedRectangle(cornerRadius: 20, style: .continuous))

                Button {
                    Task {
                        await viewModel.submitAuth()
                    }
                } label: {
                    HStack {
                        if viewModel.isAuthenticating {
                            ProgressView()
                                .tint(.white)
                        }

                        Text(viewModel.mode.buttonTitle)
                            .fontWeight(.semibold)
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 14)
                }
                .buttonStyle(.borderedProminent)
                .tint(.red)
                .disabled(viewModel.isAuthenticating || !viewModel.canSubmitAuth)

                StatusCard(
                    title: "Status",
                    icon: viewModel.statusIcon,
                    message: viewModel.statusMessage
                )

                if let authResult = viewModel.authResult {
                    TokenCard(authResult: authResult)
                }

                if let registerResult = viewModel.registerResult {
                    VStack(alignment: .leading, spacing: 10) {
                        Text("Utworzono użytkownika")
                            .font(.headline)
                        Text(registerResult.userId.uuidString)
                            .font(.footnote.monospaced())
                            .textSelection(.enabled)
                    }
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(18)
                    .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 20, style: .continuous))
                }
            }
            .padding(24)
        }
    }
}
