import SwiftUI

struct StatusCard: View {
    let title: String
    let icon: String
    let message: String

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            Label(title, systemImage: icon)
                .font(.headline)
            Text(message)
                .foregroundStyle(.secondary)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(18)
        .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 20, style: .continuous))
    }
}

struct TokenCard: View {
    let authResult: LoginResponse

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("Access token")
                .font(.subheadline.weight(.semibold))
            Text(authResult.accessToken)
                .font(.footnote.monospaced())
                .textSelection(.enabled)
                .lineLimit(4)

            Text("Refresh token")
                .font(.subheadline.weight(.semibold))
            Text(authResult.refreshToken)
                .font(.footnote.monospaced())
                .textSelection(.enabled)
                .lineLimit(3)

            Text("Wygasa: \(authResult.accessTokenExpiresAtUtc.formatted(date: .abbreviated, time: .standard))")
                .font(.footnote)
                .foregroundStyle(.secondary)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(18)
        .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 20, style: .continuous))
    }
}
