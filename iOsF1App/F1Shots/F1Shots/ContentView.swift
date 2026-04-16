import SwiftUI

struct ContentView: View {
    @State private var viewModel = AppViewModel()

    var body: some View {
        NavigationStack {
            Group {
                if viewModel.session == nil {
                    AuthScreen(viewModel: viewModel)
                } else {
                    HomeScreen(viewModel: viewModel)
                }
            }
            .background(
                LinearGradient(
                    colors: [.red.opacity(0.14), .white, .gray.opacity(0.08)],
                    startPoint: .topLeading,
                    endPoint: .bottomTrailing
                )
                .ignoresSafeArea()
            )
            .navigationTitle("F1Shots")
        }
    }
}

#Preview {
    ContentView()
}
