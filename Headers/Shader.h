#pragma once
#include "Render.h"
#include "Material.h"

#include <glad/glad.h>
#include <glm/glm.hpp>
#include <glm/gtc/type_ptr.hpp>
#include <iostream>
#include <fstream>
#include <string>
#include <sstream>
class Shader
{
private:
	const std::string vs_file, fs_file, gs_file, tcs_file, tes_file;
	unsigned int program;

public:
	Shader() = default;
	Shader(const std::string& vs_file, const std::string& fs_file, const std::string& gs_file = "", const std::string& tcs_file = "", const std::string& tes_file = "");
	Shader& operator=(const Shader&);

	void Bind();

	unsigned int GetProgram();

	void SetUniform2v(const std::string& name, const glm::vec2 valuse);
	void SetUniform3v(const std::string& name, const glm::vec3 valuse);
	void SetUniform3f(const std::string& name, const float* values);
	void SetUniform3f(const std::string& name, std::initializer_list<float> values);
	void SetUniform4f(const std::string& name, const float* values);
	void SetUniform1i(const std::string& name, const int value);
	void SetUniform1f(const std::string& name, const float value);
	void SetUniformMat4(const std::string& name, const float* values);
	void SetUniformMat4(const std::string& name, glm::mat4 values);

	void SetUniformfs(const std::string& name, const unsigned int num, const float* values);
	void SetUniformis(const std::string& name, const unsigned int num, const int* values);

	void SetUniformMateria(const std::string& name, Material& material);
	void SetUniformMateriaWithMaterial(const std::string& name, Material& material);

private:

	unsigned int CreateProgram(const std::string& vertex_source, const std::string& fragment_source, const std::string& geometry_source, const std::string& tcs_source, const std::string& tes_source);
	unsigned int CompileShader(const std::string& shader_name, unsigned int type);
	std::string ReadShaderFiles(const std::string& shader_file);
	int GetLocation(const std::string& uniform_name);
};
