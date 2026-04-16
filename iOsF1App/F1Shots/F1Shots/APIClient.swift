import Foundation

struct APIClient {
    let baseURLString: String
    var accessToken: String? = nil

    func get<Response: Decodable>(path: String) async throws -> Response {
        try await send(path: path, method: "GET", body: Optional<String>.none)
    }

    func post<Request: Encodable, Response: Decodable>(path: String, body: Request) async throws -> Response {
        try await send(path: path, method: "POST", body: body)
    }

    func patch<Request: Encodable, Response: Decodable>(path: String, body: Request) async throws -> Response {
        try await send(path: path, method: "PATCH", body: body)
    }

    func postWithoutResponse(path: String) async throws {
        _ = try await send(path: path, method: "POST", body: Optional<String>.none) as EmptyResponse
    }

    private func send<Request: Encodable, Response: Decodable>(
        path: String,
        method: String,
        body: Request?
    ) async throws -> Response {
        guard let url = URL(string: baseURLString + path) else {
            throw APIError.invalidURL
        }

        var request = URLRequest(url: url)
        request.httpMethod = method
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        if let accessToken {
            request.setValue("Bearer \(accessToken)", forHTTPHeaderField: "Authorization")
        }

        if let body {
            request.httpBody = try JSONEncoder.apiEncoder.encode(body)
        }

        let (data, response) = try await URLSession.shared.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.invalidResponse
        }

        guard (200...299).contains(httpResponse.statusCode) else {
            let decoder = JSONDecoder.apiDecoder

            if let apiMessage = try? decoder.decode(APIMessageResponse.self, from: data) {
                throw APIError.server(apiMessage.message)
            }

            throw APIError.server("Serwer zwrócił status \(httpResponse.statusCode).")
        }

        if Response.self == EmptyResponse.self {
            return EmptyResponse() as! Response
        }

        return try JSONDecoder.apiDecoder.decode(Response.self, from: data)
    }
}

enum APIError: LocalizedError {
    case invalidURL
    case invalidResponse
    case server(String)

    static func describe(_ error: Error) -> String {
        if let apiError = error as? APIError {
            return apiError.errorDescription ?? "Wystąpił nieznany błąd API."
        }

        if let urlError = error as? URLError {
            switch urlError.code {
            case .cannotConnectToHost, .timedOut, .networkConnectionLost, .notConnectedToInternet:
                return "Nie mogę połączyć się z backendem na http://localhost:5016. Upewnij się, że API działa lokalnie."
            default:
                return urlError.localizedDescription
            }
        }

        return error.localizedDescription
    }

    var errorDescription: String? {
        switch self {
        case .invalidURL:
            "Nieprawidłowy adres API."
        case .invalidResponse:
            "Serwer zwrócił nieprawidłową odpowiedź."
        case let .server(message):
            message
        }
    }
}

extension JSONEncoder {
    static var apiEncoder: JSONEncoder {
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        return encoder
    }
}

extension JSONDecoder {
    static var apiDecoder: JSONDecoder {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return decoder
    }
}

extension String {
    var trimmed: String {
        trimmingCharacters(in: .whitespacesAndNewlines)
    }
}
