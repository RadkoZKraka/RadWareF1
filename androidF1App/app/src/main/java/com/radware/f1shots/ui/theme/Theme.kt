package com.radware.f1shots.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val DarkColorScheme = darkColorScheme(
    primary = RacingRed,
    secondary = Gold,
    tertiary = Cloud,
    background = Asphalt,
    surface = Carbon,
    onPrimary = Smoke,
    onSecondary = Asphalt,
    onBackground = Smoke,
    onSurface = Smoke
)

private val LightColorScheme = lightColorScheme(
    primary = RacingRed,
    secondary = Gold,
    tertiary = Carbon,
    background = Smoke,
    surface = Color.White,
    onPrimary = Smoke,
    onSecondary = Asphalt,
    onBackground = Asphalt,
    onSurface = Asphalt
)

@Composable
fun F1ShotsAndroidTheme(
    darkTheme: Boolean = false,
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme

    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography,
        content = content
    )
}
