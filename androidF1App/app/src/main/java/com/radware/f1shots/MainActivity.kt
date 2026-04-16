package com.radware.f1shots

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import com.radware.f1shots.ui.theme.F1ShotsAndroidTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            F1ShotsAndroidTheme {
                Surface(modifier = Modifier.fillMaxSize()) {
                    F1ShotsApp()
                }
            }
        }
    }
}
