package com.radware.f1shots

import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.google.gson.JsonDeserializationContext
import com.google.gson.JsonDeserializer
import com.google.gson.JsonElement
import com.google.gson.JsonPrimitive
import com.google.gson.JsonSerializationContext
import com.google.gson.JsonSerializer
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.IOException
import java.lang.reflect.Type

class ApiClient(
    baseUrl: String,
    accessToken: String? = null
) {
    @PublishedApi
    internal val baseUrl: String = baseUrl

    @PublishedApi
    internal val accessToken: String? = accessToken

    @PublishedApi
    internal val gson: Gson = GsonBuilder()
        .registerTypeAdapter(GroupVisibility::class.java, GroupVisibilityAdapter())
        .registerTypeAdapter(GroupRole::class.java, GroupRoleAdapter())
        .create()

    @PublishedApi
    internal val client = OkHttpClient()

    @PublishedApi
    internal val jsonMediaType = "application/json; charset=utf-8".toMediaType()

    inline fun <reified T> get(path: String): T = execute(path, "GET", null)

    inline fun <reified T, reified R> post(path: String, body: T): R = execute(path, "POST", gson.toJson(body))

    inline fun <reified T, reified R> patch(path: String, body: T): R = execute(path, "PATCH", gson.toJson(body))

    fun postWithoutResponse(path: String) {
        execute<Unit>(path, "POST", "{}")
    }

    inline fun <reified R> execute(path: String, method: String, body: String?): R {
        val builder = Request.Builder()
            .url(baseUrl + path)
            .method(
                method,
                when {
                    body == null && (method == "POST" || method == "PATCH") -> "{}".toRequestBody(jsonMediaType)
                    body != null -> body.toRequestBody(jsonMediaType)
                    else -> null
                }
            )
            .addHeader("Content-Type", "application/json")

        accessToken?.let {
            builder.addHeader("Authorization", "Bearer $it")
        }

        val response = try {
            client.newCall(builder.build()).execute()
        } catch (exception: IOException) {
            throw ApiException("Nie mogę połączyć się z backendem na $baseUrl. Upewnij się, że API działa lokalnie.", exception)
        }

        response.use {
            val responseBody = it.body?.string().orEmpty()

            if (!it.isSuccessful) {
                val message = runCatching {
                    gson.fromJson(responseBody, ApiMessageResponse::class.java)?.message
                }.getOrNull()

                throw ApiException(message ?: "Serwer zwrócił status ${it.code}.")
            }

            if (R::class == Unit::class) {
                @Suppress("UNCHECKED_CAST")
                return Unit as R
            }

            return try {
                gson.fromJson(responseBody, R::class.java)
            } catch (exception: Exception) {
                throw ApiException("Nie udało się odczytać odpowiedzi serwera.", exception)
            }
        }
    }
}

class ApiException(message: String, cause: Throwable? = null) : Exception(message, cause)

class GroupVisibilityAdapter : JsonSerializer<GroupVisibility>, JsonDeserializer<GroupVisibility> {
    override fun serialize(src: GroupVisibility?, typeOfSrc: Type?, context: JsonSerializationContext?): JsonElement {
        return JsonPrimitive(src?.ordinal ?: 0)
    }

    override fun deserialize(json: JsonElement?, typeOfT: Type?, context: JsonDeserializationContext?): GroupVisibility {
        return GroupVisibility.fromValue(json?.asInt ?: 0)
    }
}

class GroupRoleAdapter : JsonSerializer<GroupRole>, JsonDeserializer<GroupRole> {
    override fun serialize(src: GroupRole?, typeOfSrc: Type?, context: JsonSerializationContext?): JsonElement {
        return JsonPrimitive(src?.ordinal ?: 0)
    }

    override fun deserialize(json: JsonElement?, typeOfT: Type?, context: JsonDeserializationContext?): GroupRole {
        return GroupRole.fromValue(json?.asInt ?: 0)
    }
}
