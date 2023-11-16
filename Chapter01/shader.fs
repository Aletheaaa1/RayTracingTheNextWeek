#version 460

in vec2 texCoord;

uniform float time0 = 0.0;
uniform float time1 = 1.0;
uniform vec2 screenSize;
uniform vec3 cameraPos;
uniform vec3 cameraTarget;
uniform vec3 cameraUp;

out vec4 fragColor;

// -------------------------------------

//	����
const int MAT_LAMBERTIAN = 0;
const int MAT_METALLIC = 1;
const int MAT_DIELECTRIC = 2;
const int MAT_PBR = 3;

struct Lambertian
{
	vec3 albedo;
};
struct Metallic
{
	vec3 albedo;
	float roughness;
};
struct Dielectric
{
	vec3 albedo;
	//	��ɫ��
    float ior;
	float roughness;
};

Lambertian lambertMaterials[4];
Metallic metallicMaterials[4];
Dielectric dielectricMaterials[4];

Lambertian LambertianConstructor(vec3 albedo)
{
	Lambertian lambertian;

	lambertian.albedo = albedo;

	return lambertian;
}

Metallic MetallicConstructor(vec3 albedo, float roughness)
{
	Metallic metallic;

	metallic.albedo = albedo;
	metallic.roughness = roughness;

	return metallic;
}

Dielectric DielectricConstructor(vec3 albedo, float roughness, float ior)
{
	Dielectric dielectric;

	dielectric.albedo = albedo;
	dielectric.roughness = roughness;
	dielectric.ior = ior;

	return dielectric;
}

////////////////////////////////////////////////////
//	�����
const float PI = 3.1415926;

uint m_u = uint(521288629);
uint m_v = uint(362436069);

uint GetUintCore(inout uint u, inout uint v)
{
	v = uint(36969) * (v & uint(65535)) + (v >> 16);
	u = uint(18000) * (u & uint(65535)) + (u >> 16);
	return (v << 16) + u;
}

float GetUniformCore(inout uint u, inout uint v)
{
	uint z = GetUintCore(u, v);

	return float(z) / uint(4294967295);
}

float rand()
{
	return GetUniformCore(m_u, m_v);
}

vec3 random_in_unit_sphere()
{
	vec3 p;

	float theta = rand() * 2.0 * PI;
	float phi   = rand() * PI;
	p.y = cos(phi);
	p.x = sin(phi) * cos(theta);
	p.z = sin(phi) * sin(theta);

	return p;
}
////////////////////////////////////////////////////

// -------------------------------------

struct Camera
{
	vec3 lower_left_corner;
	vec3 horizontal;
	vec3 vertical;
	vec3 origin;
	float time0;
	float time1;
};

struct Ray
{
	vec3 origin;
	vec3 direction;
	float time;
};

Ray NewRay(vec3 origin, vec3 direction, float time0, float time1)
{
	Ray ray;
	ray.origin = origin;
	ray.direction = normalize(direction);
	ray.time = time0 +  rand() * (time1 - time0);
	return ray;
}

struct Sphere
{
	vec3 center;
	float radius;
	//	����
	int materialType;
	int material;
};

//	�˶�����
struct SphereMoving
{
	vec3 center0;
	vec3 center1;
	float radius;
	//	����
	int materialType;
	int material;
	float time0;
	float time1;
};

Sphere NewSphere(vec3 center, float radius, int materialType, int material)
{
	Sphere sphere;
	sphere.center = center;
	sphere.radius = radius;
	sphere.materialType = materialType;
	sphere.material = material;

	return sphere;
}

SphereMoving NewMovingSphere(vec3 center0, vec3 center1, float radius, float time0, float time1,int materialType, int material)
{
	SphereMoving sphere;
	sphere.center0 = center0;
	sphere.center1 = center1;
	sphere.time0 = time0;
	sphere.time1 = time1;
	sphere.radius = radius;
	sphere.materialType = materialType;
	sphere.material = material;

	return sphere;
}

struct World
{
	int objectNumber;
	Sphere objects[10];

	int movingNumber;
	SphereMoving movingObjects[10];
};

World NewWorld()
{
	World world;
	world.objectNumber = 7;
	world.objects[0] = NewSphere(vec3(0.0, 0.0, -1.0), 0.5, MAT_LAMBERTIAN, 2);
	world.objects[1] = NewSphere(vec3(1.0, 1.0, -2.0), 0.5, MAT_DIELECTRIC, 2);
	world.objects[2] = NewSphere(vec3(-1.0, 0.0, -0.0), 0.5, MAT_METALLIC, 2);
	world.objects[3] = NewSphere(vec3(0.0, -100.5, -4.0), 100.0, MAT_LAMBERTIAN, 3);
	world.objects[4] = NewSphere(vec3(-4.0, 0.0, -2.0), 1.2, MAT_METALLIC, 1);
	world.objects[5] = NewSphere(vec3(0.0, 3.0, -5.0), 3.0, MAT_DIELECTRIC, 2);
	world.objects[6] = NewSphere(vec3(-6.0, 3.0, -5.0), 3.0, MAT_METALLIC, 2);

	world.movingNumber = 2;
	world.movingObjects[0] = NewMovingSphere(vec3(-2.0, 0.0, -2.0), vec3(-2.0, 0.5, -2.0), 0.55, time0, time1, MAT_LAMBERTIAN, 0);
	world.movingObjects[1] = NewMovingSphere(vec3(-3.0, 0.0, -0.2), vec3(-3.0, 0.5, -0.0), 0.25, time0, time1, MAT_DIELECTRIC, 3);
	return world;
}

World world;

struct HitRecord
{
	float t;
	vec3 position;
	vec3 normal;
	int materialType;
	int material;
};

////////////////////////////////////////////////////

//////////////////////////////////////////////////////
//	Lambertģ��ɢ��
bool LambertianScatter(in Lambertian lambertian, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	attenuation = lambertian.albedo;

	scattered.origin = hitRecord.position;
	scattered.direction = hitRecord.normal + random_in_unit_sphere();
	scattered.time = incident.time;

	return true;
}

//	��������ɢ��
bool MetallicScatter(in Metallic metallic, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	attenuation = metallic.albedo;

	scattered.origin = hitRecord.position;
	scattered.direction = reflect(incident.direction, hitRecord.normal) + metallic.roughness * random_in_unit_sphere() / 2.0;
	scattered.time = incident.time;

	return dot(scattered.direction, hitRecord.normal) > 0.0;
}

bool Refract(vec3 v, vec3 n, float ni_over_nt, out vec3 refracted)
{
	vec3 uv = normalize(v);
	float cos_theta = abs(dot(uv, n));
	float sin_theta = sqrt(1.0 - cos_theta * cos_theta);

	if(ni_over_nt * sin_theta < 1.0)
	{
		vec3 refracted_h = ni_over_nt * (uv + n * cos_theta);
		vec3 refracted_v = -1 * n * sqrt(1.0 - dot(refracted_h, refracted_h));
		refracted = refracted_h + refracted_v;

		return true;
	}
	else
	{
		return false;
	}

}

float Schlick(float cosine, float ior)
{
	float r0 = (1.0 - ior) / (1.0 + ior);
	r0 = r0 * r0;
	return r0 + (1 - r0) * pow((1 - cosine), 5.0);
}

//	��Ե��ɢ��
bool DielectricScatter(in Dielectric dielectric, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	float ni_over_nt = 0.0;
	vec3 normal = vec3(0.0);

	if(dot(incident.direction, hitRecord.normal) > 0.0)			//	�ڲ�
	{
		ni_over_nt = dielectric.ior;
		normal = -hitRecord.normal;
	}
	else
	{
		ni_over_nt = 1.0 / dielectric.ior;
		normal = hitRecord.normal;
	}

	vec3 refracted;
	attenuation = dielectric.albedo;

	if(Refract(incident.direction, normal, ni_over_nt, refracted))
	{
		scattered.origin = hitRecord.position;
		scattered.direction = refracted;
		scattered.time = incident.time;
	}
	else
	{
		scattered.origin = hitRecord.position;
		scattered.direction = reflect(incident.direction, hitRecord.normal);
		scattered.time = incident.time;
	}

	return true;
}
//	Schlick����
bool DielectricScatter2(in Dielectric dielectric, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	float ni_over_nt = 0.0;
	vec3 normal = vec3(0.0);
	float cosine = 0.0;

	if(dot(incident.direction, hitRecord.normal) > 0.0)			//	�ڲ�
	{
		ni_over_nt = dielectric.ior;
		normal = -hitRecord.normal;
		cosine = dot(incident.direction, hitRecord.normal);
	}
	else
	{
		ni_over_nt = 1.0 / dielectric.ior;
		normal = hitRecord.normal;
		cosine = dot(incident.direction, -hitRecord.normal);
	}

	vec3 refracted;
	attenuation = dielectric.albedo;

	float schlick_ration;
	if(Refract(incident.direction, normal, ni_over_nt, refracted))
	{
		schlick_ration = Schlick(cosine, ni_over_nt);
	}
	else
	{
		schlick_ration = 1.0;
	}

	if(schlick_ration < rand())
	{
		scattered.origin = hitRecord.position;
		scattered.direction = refracted;
		scattered.time = incident.time;
	}
	else
	{
		scattered.origin = hitRecord.position;
		scattered.direction = refracted;
		scattered.time = incident.time;
//		scattered.origin = hitRecord.position;
//		scattered.direction = reflect(incident.direction, hitRecord.normal) + dielectric.roughness * random_in_unit_sphere();
	}

	return true;
}

bool MaterialScatter(in int materialType, in int material, in Ray incident, in HitRecord hitRecord, out Ray scatter, out vec3 attenuation)
{
	if(materialType == MAT_LAMBERTIAN)
	{
		return LambertianScatter(lambertMaterials[material] , incident, hitRecord, scatter, attenuation);
	}
	else if(materialType == MAT_METALLIC)
	{
		return MetallicScatter(metallicMaterials[material], incident, hitRecord, scatter, attenuation);
	}
	else if(materialType == MAT_DIELECTRIC){
		return DielectricScatter2(dielectricMaterials[material], incident, hitRecord, scatter, attenuation);
	}

	return false;
}
/////////////////////////////////////////////////////

Camera camera;

Camera CameraSet(vec3 eye, vec3 target, vec3 up, float vfov, float aspect, float time0, float time1)
{
	Camera camera;

	float halfHeight = tan((vfov / 2.0) * PI / 180.0);
	float halfWidth = halfHeight * aspect;

	vec3 zAxis = normalize(eye - target);
	vec3 xAxis = cross(up, zAxis);
	vec3 yAxis = cross(zAxis, xAxis);

	camera.origin = eye;
	camera.horizontal = 2.0 * halfWidth * xAxis;
	camera.vertical = 2.0 * halfHeight * yAxis;
	camera.lower_left_corner = camera.origin - camera.horizontal / 2.0 - camera.vertical / 2.0 - zAxis;
	camera.time0 = time0;
	camera.time1 = time1;
	return camera;
}

void InitScience()
{
	lambertMaterials[0] = LambertianConstructor(vec3(0.7, 0.5, 0.5));
	lambertMaterials[1] = LambertianConstructor(vec3(0.5, 0.7, 0.5));
	lambertMaterials[2] = LambertianConstructor(vec3(0.5, 0.5, 0.7));
	lambertMaterials[3] = LambertianConstructor(vec3(0.7, 0.7, 0.1));

	metallicMaterials[0] = MetallicConstructor(vec3(0.7, 0.5, 0.5), 0.0);
	metallicMaterials[1] = MetallicConstructor(vec3(0.5, 0.7, 0.5), 0.3);
	metallicMaterials[2] = MetallicConstructor(vec3(0.2, 0.8, 0.8), 1.0);
	metallicMaterials[3] = MetallicConstructor(vec3(0.7, 0.7, 0.7), 0.3);

	dielectricMaterials[0] = DielectricConstructor(vec3(1.0, 1.0, 1.0), 0.0, 1.5);
	dielectricMaterials[1] = DielectricConstructor(vec3(1.0, 1.0, 1.0), 0.1, 2.5);
	dielectricMaterials[2] = DielectricConstructor(vec3(1.0, 1.0, 1.0), 0.9, 1.5);
	dielectricMaterials[3] = DielectricConstructor(vec3(1.0, 1.0, 1.0), 0.3, 1.5);

	camera = CameraSet(cameraPos, cameraTarget, cameraUp, 90.0f, float(screenSize.x) / float(screenSize.y), time0, time1);
	world = NewWorld();
}

//	������߽���
vec3 RayGetPointAt(Ray ray, float t)
{
	return ray.origin + t * ray.direction;
}

//	�жϹ����������ཻ
bool SphereHit(Sphere sphere, Ray ray, float t_min, float t_max, inout HitRecord rec)
{
	vec3 oc = ray.origin - sphere.center;

	float a = dot(ray.direction, ray.direction);
	float b = dot(oc, ray.direction);
	float c = dot(oc, oc) - sphere.radius * sphere.radius;

	float discriminant = b * b - a * c;
	if(discriminant > 0.0)
	{
		float temp = (-b - sqrt(discriminant)) / (a);
		if(temp < t_max && temp > t_min)
		{
			rec.t = temp;
			rec.position = RayGetPointAt(ray, rec.t);
			rec.normal = normalize(rec.position - sphere.center);
			rec.materialType = sphere.materialType;
			rec.material = sphere.material;

			return true;
		}

		temp = (-b + sqrt(discriminant)) / (a);
		if(temp < t_max && temp > t_min)
		{
			rec.t = temp;
			rec.position = RayGetPointAt(ray, rec.t);
			rec.normal = normalize(rec.position - sphere.center);
			rec.materialType = sphere.materialType;
			rec.material = sphere.material;

			return true;
		}
	}

	return false;
}

//	�ƶ������ཻ
bool MovingSphereHit(SphereMoving sphere, Ray ray, float t_min, float t_max, inout HitRecord rec)
{
	vec3 moving_center = sphere.center0 + ray.time * (sphere.center1 - sphere.center0);
	vec3 oc = ray.origin - moving_center;

	float a = dot(ray.direction, ray.direction);
	float b = dot(oc, ray.direction);
	float c = dot(oc, oc) - sphere.radius * sphere.radius;

	float discriminant = b * b - a * c;
	if(discriminant > 0.0)
	{
		float temp = (-b - sqrt(discriminant)) / (a);
		if(temp < t_max && temp > t_min)
		{
			rec.t = temp;
			rec.position = RayGetPointAt(ray, rec.t);
			rec.normal =normalize(rec.position - moving_center);
			rec.materialType = sphere.materialType;
			rec.material = sphere.material;

			return true;
		}

		temp = (-b + sqrt(discriminant)) / (a);
		if(temp < t_max && temp > t_min)
		{
			rec.t = temp;
			rec.position = RayGetPointAt(ray, rec.t);
			rec.normal = normalize(rec.position - moving_center);
			rec.materialType = sphere.materialType;
			rec.material = sphere.material;

			return true;
		}
	}

	return false;
}

//	��¼world�����й����ཻ������Ϣ
bool WorldHit(World world, Ray ray, float t_min, float t_max, inout HitRecord rec)
{
	HitRecord temRec;
	float cloestSoFar = t_max;
	bool hitSomething = false;

	for(int i=0; i<world.objectNumber; i++)
	{
		if(SphereHit(world.objects[i], ray, t_min, cloestSoFar, temRec))
		{
			rec = temRec;
			cloestSoFar = rec.t;
			hitSomething = true;
		}

		if(MovingSphereHit(world.movingObjects[i], ray, t_min, cloestSoFar, temRec))
		{
			rec = temRec;
			cloestSoFar = rec.t;
			hitSomething = true;
		}
	}

	return hitSomething;
}

//	����׷��
vec3 WorldTrace(Ray ray, World world, int depth)
{
	HitRecord hitRecord;

	vec3 frac = vec3(1.0);
	vec3 bgColor = vec3(0.0);

	while(depth > 0)
	{
		depth--;
		if(WorldHit(world, ray, 0.001, 10000.0, hitRecord))
		{
			Ray scaterRay;
			vec3 attenuation;
			if(!MaterialScatter(hitRecord.materialType, hitRecord.material, ray, hitRecord, scaterRay, attenuation))
			{
				break;
			}

			frac *= attenuation;
			ray = scaterRay;
		}
		else
		{
			//	������ɫ
			vec3 dir = normalize(ray.direction);
			float t = (dir.y + 1.0) / 2.0;
			bgColor =  mix(vec3(1.0), vec3(0.5, 0.7, 1.0), t);
			break;
		}
	}

	return bgColor * frac;
}
Ray AARay(Camera camera, vec2 offset)
{
	Ray ray = NewRay(camera.origin ,
		camera.lower_left_corner +
		offset.x * camera.horizontal +
		offset.y * camera.vertical - camera.origin,
		time0,
		time1);

	return ray;
}

void main()
{
	float u = texCoord.x;
	float v = texCoord.y;

	InitScience();

	vec3 col = vec3(0.0);

	vec2 texSize = 1.0 / screenSize;
	int ns = 1000;
	for(int i=0; i<ns; i++)
	{
		Ray ray = AARay(camera, texCoord + vec2(rand(), rand()) * texSize);
		col += WorldTrace(ray, world, 50);
	}
	col /= ns;
	col = pow(col, vec3(1.0 / 2.0));

	fragColor = vec4(col, 1.0);
}