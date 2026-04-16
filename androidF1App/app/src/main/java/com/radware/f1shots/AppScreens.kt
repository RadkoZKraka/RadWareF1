package com.radware.f1shots

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Divider
import androidx.compose.material3.FilterChip
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.radware.f1shots.ui.theme.F1ShotsAndroidTheme

@Composable
fun F1ShotsApp(viewModel: AppViewModel = viewModel()) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(
                Brush.linearGradient(
                    colors = listOf(
                        Color(0xFFFFE2E2),
                        Color(0xFFFFFFFF),
                        Color(0xFFF0F3F5)
                    )
                )
            )
            .statusBarsPadding()
    ) {
        if (viewModel.session == null) {
            AuthScreen(viewModel)
        } else {
            HomeScreen(viewModel)
        }
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun AuthScreen(viewModel: AppViewModel) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(18.dp)
    ) {
        Text("F1Shots", style = MaterialTheme.typography.headlineLarge, fontWeight = FontWeight.Bold)
        Text("Przetestuj lokalnie rejestrację i logowanie do backendu RadWareF1.", color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text("API: ${viewModel.baseUrl}", style = MaterialTheme.typography.bodySmall)

        FlowRow(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
            AuthMode.entries.forEach { mode ->
                FilterChip(
                    selected = viewModel.mode == mode,
                    onClick = {
                        viewModel.mode = mode
                        viewModel.statusMessage = if (mode == AuthMode.Register) {
                            "Wpisz dane i utwórz konto w lokalnym API."
                        } else {
                            "Wpisz dane istniejącego konta i pobierz token JWT."
                        }
                    },
                    label = { Text(mode.title) }
                )
            }
        }

        F1Card {
            if (viewModel.mode == AuthMode.Register) {
                AppTextField(
                    value = viewModel.userName,
                    onValueChange = { viewModel.userName = it },
                    label = "User name"
                )
                Spacer(Modifier.height(10.dp))
            }

            AppTextField(
                value = viewModel.email,
                onValueChange = { viewModel.email = it },
                label = "Email",
                keyboardType = KeyboardType.Email
            )
            Spacer(Modifier.height(10.dp))
            OutlinedTextField(
                value = viewModel.password,
                onValueChange = { viewModel.password = it },
                label = { Text("Password") },
                modifier = Modifier.fillMaxWidth(),
                visualTransformation = PasswordVisualTransformation(),
                singleLine = true
            )
        }

        Button(
            onClick = { viewModel.submitAuth() },
            modifier = Modifier.fillMaxWidth(),
            enabled = !viewModel.isAuthenticating && viewModel.canSubmitAuth
        ) {
            if (viewModel.isAuthenticating) {
                CircularProgressIndicator(color = MaterialTheme.colorScheme.onPrimary, modifier = Modifier.height(18.dp))
            } else {
                Text(viewModel.mode.buttonTitle)
            }
        }

        StatusCard(title = "Status", message = viewModel.statusMessage)

        viewModel.authResult?.let { authResult ->
            F1Card {
                Text("Access token", fontWeight = FontWeight.SemiBold)
                Text(authResult.accessToken, style = MaterialTheme.typography.bodySmall)
                Spacer(Modifier.height(10.dp))
                Text("Refresh token", fontWeight = FontWeight.SemiBold)
                Text(authResult.refreshToken, style = MaterialTheme.typography.bodySmall)
                Spacer(Modifier.height(10.dp))
                Text("Wygasa: ${authResult.accessTokenExpiresAtUtc}", style = MaterialTheme.typography.bodySmall)
            }
        }

        viewModel.registerResult?.let { registerResult ->
            F1Card {
                Text("Utworzono użytkownika", fontWeight = FontWeight.SemiBold)
                Text(registerResult.userId, style = MaterialTheme.typography.bodySmall)
            }
        }
    }
}

@Composable
private fun HomeScreen(viewModel: AppViewModel) {
    LaunchedEffect(viewModel.session?.accessToken) {
        if (viewModel.session != null && viewModel.currentUser == null && !viewModel.isLoadingHome) {
            viewModel.refreshHome()
        }
    }

    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = androidx.compose.foundation.layout.PaddingValues(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        item {
            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                    Text("Home", style = MaterialTheme.typography.headlineLarge, fontWeight = FontWeight.Bold)
                    Text(
                        text = viewModel.currentUser?.let { "Zalogowany jako ${it.email}" } ?: "Ładowanie danych użytkownika i grup...",
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }

                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedButton(onClick = { viewModel.refreshHome() }, enabled = !viewModel.isLoadingHome) {
                        Text("Refresh")
                    }
                    OutlinedButton(onClick = { viewModel.logout() }) {
                        Text("Log out")
                    }
                }
            }
        }

        item {
            StatusCard(title = "Last action", message = viewModel.homeStatusMessage)
        }

        item {
            F1Card {
                Text("Overview", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                Spacer(Modifier.height(12.dp))
                Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                    StatTile("My groups", viewModel.myGroups.size.toString(), Modifier.weight(1f))
                    StatTile("Public groups", viewModel.publicGroups.size.toString(), Modifier.weight(1f))
                    StatTile("Members", viewModel.selectedGroupMembers.size.toString(), Modifier.weight(1f))
                }
            }
        }

        item {
            F1Card {
                Text("Create group", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                Spacer(Modifier.height(12.dp))
                AppTextField(
                    value = viewModel.createGroupName,
                    onValueChange = { viewModel.createGroupName = it },
                    label = "Group name"
                )
                Spacer(Modifier.height(12.dp))
                VisibilityChips(
                    selected = viewModel.createVisibility,
                    onSelected = { viewModel.createVisibility = it }
                )
                Spacer(Modifier.height(12.dp))
                Button(
                    onClick = { viewModel.createGroup() },
                    enabled = !viewModel.isLoadingHome && viewModel.createGroupName.trim().isNotEmpty(),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Create")
                }
            }
        }

        item {
            F1Card {
                Text("Join public group", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                Spacer(Modifier.height(12.dp))
                AppTextField(
                    value = viewModel.joinCode,
                    onValueChange = { viewModel.joinCode = it },
                    label = "Join code"
                )
                Spacer(Modifier.height(12.dp))
                OutlinedButton(
                    onClick = { viewModel.joinGroup() },
                    enabled = !viewModel.isLoadingHome && viewModel.joinCode.trim().isNotEmpty(),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Join with code")
                }
            }
        }

        item {
            SectionTitle("My Groups")
        }

        if (viewModel.myGroups.isEmpty()) {
            item { EmptyCard("Nie masz jeszcze żadnej grupy. Stwórz własną albo dołącz kodem.") }
        } else {
            items(viewModel.myGroups, key = { "my-${it.id}" }) { group ->
                GroupRow(
                    title = group.name,
                    subtitle = "${group.visibility.title} • ${group.membersCount} members",
                    actionLabel = if (viewModel.selectedGroupDetails?.id == group.id) "Selected" else "Open",
                    onAction = { viewModel.selectGroup(group.id) }
                )
            }
        }

        item {
            SectionTitle("Public Groups")
        }

        if (viewModel.publicGroups.isEmpty()) {
            item { EmptyCard("Brak publicznych grup do wyświetlenia.") }
        } else {
            items(viewModel.publicGroups, key = { "public-${it.id}" }) { group ->
                GroupRow(
                    title = group.name,
                    subtitle = "${group.membersCount} members",
                    actionLabel = "Details",
                    onAction = { viewModel.selectGroup(group.id) }
                )
            }
        }

        viewModel.selectedGroupDetails?.let { details ->
            item {
                F1Card {
                    Text("Selected Group", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                    Spacer(Modifier.height(10.dp))
                    Text(details.name, style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
                    Text("${details.visibility.title} • ${details.membersCount} members")
                    Text("Created: ${details.createdAtUtc}", style = MaterialTheme.typography.bodySmall)

                    viewModel.latestJoinCodeByGroupId[details.id]?.let { code ->
                        Spacer(Modifier.height(8.dp))
                        Text("Join code: $code", style = MaterialTheme.typography.bodySmall)
                    }

                    Spacer(Modifier.height(14.dp))
                    Divider()
                    Spacer(Modifier.height(14.dp))
                    Text("Members", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                    Spacer(Modifier.height(8.dp))
                    if (viewModel.selectedGroupMembers.isEmpty()) {
                        Text("Brak członków do wyświetlenia albo lista jeszcze się ładuje.")
                    } else {
                        viewModel.selectedGroupMembers.forEach { member ->
                            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                                Text(member.userId, modifier = Modifier.weight(1f), style = MaterialTheme.typography.bodySmall)
                                Text(member.role.title)
                            }
                            Spacer(Modifier.height(6.dp))
                        }
                    }

                    Spacer(Modifier.height(14.dp))
                    Text("Update group", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                    Spacer(Modifier.height(10.dp))
                    AppTextField(
                        value = viewModel.updateGroupName,
                        onValueChange = { viewModel.updateGroupName = it },
                        label = "New group name"
                    )
                    Spacer(Modifier.height(12.dp))
                    VisibilityChips(
                        selected = viewModel.updateVisibility,
                        onSelected = { viewModel.updateVisibility = it }
                    )
                    Spacer(Modifier.height(12.dp))
                    OutlinedButton(
                        onClick = { viewModel.updateSelectedGroup() },
                        enabled = !viewModel.isLoadingHome,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Text("Save changes")
                    }

                    if (details.isMember) {
                        Spacer(Modifier.height(10.dp))
                        Button(
                            onClick = { viewModel.leaveSelectedGroup() },
                            enabled = !viewModel.isLoadingHome,
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text("Leave group")
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun AppTextField(
    value: String,
    onValueChange: (String) -> Unit,
    label: String,
    keyboardType: KeyboardType = KeyboardType.Text
) {
    OutlinedTextField(
        value = value,
        onValueChange = onValueChange,
        label = { Text(label) },
        modifier = Modifier.fillMaxWidth(),
        singleLine = true,
        keyboardOptions = KeyboardOptions(keyboardType = keyboardType)
    )
}

@Composable
private fun VisibilityChips(
    selected: GroupVisibility,
    onSelected: (GroupVisibility) -> Unit
) {
    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        GroupVisibility.entries.forEach { visibility ->
            FilterChip(
                selected = selected == visibility,
                onClick = { onSelected(visibility) },
                label = { Text(visibility.title) }
            )
        }
    }
}

@Composable
private fun StatusCard(title: String, message: String) {
    F1Card {
        Text(title, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(8.dp))
        Text(message)
    }
}

@Composable
private fun StatTile(title: String, value: String, modifier: Modifier = Modifier) {
    Surface(
        modifier = modifier,
        shape = RoundedCornerShape(18.dp),
        color = MaterialTheme.colorScheme.surfaceVariant
    ) {
        Column(modifier = Modifier.padding(14.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Text(value, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
            Text(title, style = MaterialTheme.typography.bodySmall)
        }
    }
}

@Composable
private fun GroupRow(
    title: String,
    subtitle: String,
    actionLabel: String,
    onAction: () -> Unit
) {
    F1Card {
        Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
            Column(modifier = Modifier.weight(1f)) {
                Text(title, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                Text(subtitle, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            Spacer(Modifier.padding(6.dp))
            OutlinedButton(onClick = onAction) {
                Text(actionLabel)
            }
        }
    }
}

@Composable
private fun EmptyCard(text: String) {
    F1Card {
        Text(text, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
}

@Composable
private fun SectionTitle(text: String) {
    Text(text, style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
}

@Composable
private fun F1Card(content: @Composable ColumnScope.() -> Unit) {
    Card(
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface.copy(alpha = 0.96f)),
        shape = RoundedCornerShape(24.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(18.dp),
            verticalArrangement = Arrangement.spacedBy(4.dp),
            content = content
        )
    }
}

@Preview(showBackground = true)
@Composable
private fun AppPreview() {
    F1ShotsAndroidTheme {
        F1ShotsApp(viewModel = AppViewModel())
    }
}
