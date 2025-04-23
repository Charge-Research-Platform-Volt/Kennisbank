dev:
	TARGET=development STAGE=development docker-compose -f docker-compose.yml -f docker-compose.dev.yml up

prod:
	TARGET=production STAGE=production docker-compose -f docker-compose.yml -f docker-compose.prod.yml up

ci:
	TARGET=ci-runner STAGE=development docker-compose -f docker-compose.yml -f docker-compose.ci.yml up

down:
	docker-compose down