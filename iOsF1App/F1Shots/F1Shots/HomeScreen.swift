import SwiftUI

struct HomeScreen: View {
    @Bindable var viewModel: AppViewModel

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 20) {
                header
                overview
                createAndJoin
                myGroups
                publicGroups

                if let details = viewModel.selectedGroupDetails {
                    selectedGroup(details)
                }
            }
            .padding(24)
        }
        .task(id: viewModel.session?.accessToken) {
            guard viewModel.session != nil else { return }

            if viewModel.currentUser == nil && !viewModel.isLoadingHome {
                await viewModel.refreshHome()
            }
        }
    }

    private var header: some View {
        HStack(alignment: .top) {
            VStack(alignment: .leading, spacing: 6) {
                Text("Home")
                    .font(.largeTitle.bold())

                if let currentUser = viewModel.currentUser {
                    Text("Zalogowany jako \(currentUser.email)")
                        .foregroundStyle(.secondary)
                } else {
                    Text("Ładowanie danych użytkownika i grup...")
                        .foregroundStyle(.secondary)
                }
            }

            Spacer()

            VStack(spacing: 10) {
                Button {
                    Task {
                        await viewModel.refreshHome()
                    }
                } label: {
                    Label("Refresh", systemImage: "arrow.clockwise")
                }
                .buttonStyle(.bordered)
                .disabled(viewModel.isLoadingHome)

                Button("Log out", role: .destructive) {
                    viewModel.logout()
                }
                .buttonStyle(.bordered)
            }
        }
    }

    private var overview: some View {
        VStack(alignment: .leading, spacing: 14) {
            Text("Overview")
                .font(.headline)

            HStack(spacing: 12) {
                dashboardTile(title: "My groups", value: "\(viewModel.myGroups.count)", icon: "person.3.fill")
                dashboardTile(title: "Public groups", value: "\(viewModel.publicGroups.count)", icon: "globe")
                dashboardTile(title: "Members", value: "\(viewModel.selectedGroupMembers.count)", icon: "person.2.fill")
            }

            StatusCard(
                title: "Last action",
                icon: viewModel.homeStatusIcon,
                message: viewModel.homeStatusMessage
            )
        }
    }

    private var createAndJoin: some View {
        VStack(alignment: .leading, spacing: 14) {
            Text("Group Actions")
                .font(.headline)

            VStack(alignment: .leading, spacing: 12) {
                Text("Create group")
                    .font(.subheadline.weight(.semibold))

                TextField("Group name", text: $viewModel.createGroupName)
                    .textFieldStyle(.roundedBorder)

                Picker("Visibility", selection: $viewModel.createVisibility) {
                    ForEach(GroupVisibility.allCases) { visibility in
                        Text(visibility.title).tag(visibility)
                    }
                }
                .pickerStyle(.segmented)

                Button {
                    Task {
                        await viewModel.createGroup()
                    }
                } label: {
                    Label("Create", systemImage: "plus.circle.fill")
                        .frame(maxWidth: .infinity)
                }
                .buttonStyle(.borderedProminent)
                .tint(.red)
                .disabled(viewModel.isLoadingHome || viewModel.createGroupName.trimmed.isEmpty)
            }
            .padding(18)
            .background(.thinMaterial, in: RoundedRectangle(cornerRadius: 20, style: .continuous))

            VStack(alignment: .leading, spacing: 12) {
                Text("Join public group")
                    .font(.subheadline.weight(.semibold))

                TextField("Join code", text: $viewModel.joinCode)
                    .textInputAutocapitalization(.characters)
                    .autocorrectionDisabled()
                    .textFieldStyle(.roundedBorder)

                Button {
                    Task {
                        await viewModel.joinGroup()
                    }
                } label: {
                    Label("Join with code", systemImage: "person.badge.plus")
                        .frame(maxWidth: .infinity)
                }
                .buttonStyle(.bordered)
                .disabled(viewModel.isLoadingHome || viewModel.joinCode.trimmed.isEmpty)
            }
            .padding(18)
            .background(.thinMaterial, in: RoundedRectangle(cornerRadius: 20, style: .continuous))
        }
    }

    private var myGroups: some View {
        VStack(alignment: .leading, spacing: 14) {
            Text("My Groups")
                .font(.headline)

            if viewModel.myGroups.isEmpty {
                emptyCard(text: "Nie masz jeszcze żadnej grupy. Stwórz własną albo dołącz kodem.")
            } else {
                ForEach(viewModel.myGroups) { group in
                    Button {
                        Task {
                            await viewModel.selectGroup(group.id)
                        }
                    } label: {
                        HStack {
                            VStack(alignment: .leading, spacing: 6) {
                                Text(group.name)
                                    .font(.headline)
                                Text("\(group.visibility.title) • \(group.membersCount) members")
                                    .foregroundStyle(.secondary)
                            }

                            Spacer()

                            Image(systemName: viewModel.selectedGroupDetails?.id == group.id ? "checkmark.circle.fill" : "chevron.right.circle")
                                .foregroundStyle(.red)
                        }
                        .padding(16)
                        .frame(maxWidth: .infinity, alignment: .leading)
                    }
                    .buttonStyle(.plain)
                    .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 18, style: .continuous))
                }
            }
        }
    }

    private var publicGroups: some View {
        VStack(alignment: .leading, spacing: 14) {
            Text("Public Groups")
                .font(.headline)

            if viewModel.publicGroups.isEmpty {
                emptyCard(text: "Brak publicznych grup do wyświetlenia.")
            } else {
                ForEach(viewModel.publicGroups) { group in
                    HStack {
                        VStack(alignment: .leading, spacing: 6) {
                            Text(group.name)
                                .font(.headline)
                            Text("\(group.membersCount) members")
                                .foregroundStyle(.secondary)
                        }

                        Spacer()

                        Button {
                            Task {
                                await viewModel.selectGroup(group.id)
                            }
                        } label: {
                            Label("Details", systemImage: "info.circle")
                        }
                        .buttonStyle(.bordered)
                    }
                    .padding(16)
                    .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 18, style: .continuous))
                }
            }
        }
    }

    private func selectedGroup(_ details: GroupDetailsResponse) -> some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("Selected Group")
                .font(.headline)

            VStack(alignment: .leading, spacing: 8) {
                Text(details.name)
                    .font(.title3.weight(.semibold))
                Text("\(details.visibility.title) • \(details.membersCount) members")
                    .foregroundStyle(.secondary)
                Text("Created: \(details.createdAtUtc.formatted(date: .abbreviated, time: .omitted))")
                    .font(.footnote)
                    .foregroundStyle(.secondary)

                if let joinCode = viewModel.latestJoinCodeByGroupId[details.id] {
                    Text("Join code: \(joinCode)")
                        .font(.footnote.monospaced())
                        .textSelection(.enabled)
                }
            }

            VStack(alignment: .leading, spacing: 10) {
                Text("Members")
                    .font(.subheadline.weight(.semibold))

                if viewModel.selectedGroupMembers.isEmpty {
                    Text("Brak członków do wyświetlenia albo lista jeszcze się ładuje.")
                        .foregroundStyle(.secondary)
                } else {
                    ForEach(viewModel.selectedGroupMembers) { member in
                        HStack {
                            Text(member.userId.uuidString)
                                .font(.footnote.monospaced())
                                .lineLimit(1)
                            Spacer()
                            Text(member.role.title)
                                .foregroundStyle(.secondary)
                        }
                    }
                }
            }

            VStack(alignment: .leading, spacing: 12) {
                Text("Update group")
                    .font(.subheadline.weight(.semibold))

                TextField("New group name", text: $viewModel.updateGroupName)
                    .textFieldStyle(.roundedBorder)

                Picker("New visibility", selection: $viewModel.updateVisibility) {
                    ForEach(GroupVisibility.allCases) { visibility in
                        Text(visibility.title).tag(visibility)
                    }
                }
                .pickerStyle(.segmented)

                Button {
                    Task {
                        await viewModel.updateSelectedGroup()
                    }
                } label: {
                    Label("Save changes", systemImage: "square.and.pencil")
                        .frame(maxWidth: .infinity)
                }
                .buttonStyle(.bordered)
                .disabled(viewModel.isLoadingHome)
            }

            if details.isMember {
                Button("Leave group", role: .destructive) {
                    Task {
                        await viewModel.leaveSelectedGroup()
                    }
                }
                .buttonStyle(.borderedProminent)
                .tint(.black)
                .disabled(viewModel.isLoadingHome)
            }
        }
        .padding(20)
        .background(.thinMaterial, in: RoundedRectangle(cornerRadius: 24, style: .continuous))
    }

    private func dashboardTile(title: String, value: String, icon: String) -> some View {
        VStack(alignment: .leading, spacing: 8) {
            Image(systemName: icon)
                .foregroundStyle(.red)
            Text(value)
                .font(.title2.bold())
            Text(title)
                .foregroundStyle(.secondary)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(16)
        .background(.thinMaterial, in: RoundedRectangle(cornerRadius: 20, style: .continuous))
    }

    private func emptyCard(text: String) -> some View {
        Text(text)
            .frame(maxWidth: .infinity, alignment: .leading)
            .padding(16)
            .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 18, style: .continuous))
            .foregroundStyle(.secondary)
    }
}
